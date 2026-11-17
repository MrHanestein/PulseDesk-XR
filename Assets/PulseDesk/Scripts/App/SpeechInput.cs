using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace PulseDesk
{
    /// <summary>
    /// Speech to text through Android's own SpeechRecognizer, which on Quest is served by the headset's
    /// built-in recognition service. No SDK, no key, nothing in the APK. In the Editor it is unavailable
    /// and the caller falls back to typing.
    /// </summary>
    public class SpeechInput : MonoBehaviour
    {
        private const float ListenLimitSeconds = 15f;

        public bool Available { get; private set; }
        public bool Listening { get; private set; }

        private Action<string> _partial;
        private Action<string> _final;
        private Action<string> _failed;
        private float _deadline;
        private readonly Queue<Action> _main = new Queue<Action>();
        private readonly object _lock = new object();

        private void Post(Action action)
        {
            lock (_lock) _main.Enqueue(action);
        }

        /// <summary>partial gets live text while you speak, final the finished line, failed a message to show.</summary>
        public void Listen(Action<string> partial, Action<string> final, Action<string> failed)
        {
            _partial = partial;
            _final = final;
            _failed = failed;
            Listening = true;
            _deadline = Time.unscaledTime + ListenLimitSeconds;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                PermissionCallbacks callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => Post(StartRecognizer);
                callbacks.PermissionDenied += _ => Post(() => Fail("The microphone is off for PulseDesk. Type instead"));
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
                return;
            }
            StartRecognizer();
#else
            Fail("Speaking works on the headset. Type instead");
#endif
        }

        /// <summary>Stop listening and use what was heard so far.</summary>
        public void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            CallOnUi("stopListening");
#else
            Listening = false;
#endif
        }

        private void Fail(string message)
        {
            Listening = false;
            Action<string> failed = _failed;
            if (failed != null) failed(message);
        }

        private void Update()
        {
            while (true)
            {
                Action next;
                lock (_lock)
                {
                    if (_main.Count == 0) break;
                    next = _main.Dequeue();
                }
                try { next(); }
                catch (Exception e) { Debug.LogWarning("PulseDesk: speech callback failed. " + e.Message); }
            }

            if (Listening && Time.unscaledTime > _deadline)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                CallOnUi("cancel");
#endif
                Fail("Did not hear anything. Pinch Speak and try again");
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _activity;
        private AndroidJavaObject _recognizer;
        private Listener _listener;

        private void Start()
        {
            try
            {
                _activity = AndroidApplication.currentActivity;
                using (AndroidJavaClass sr = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                    Available = sr.CallStatic<bool>("isRecognitionAvailable", _activity);
            }
            catch (Exception e)
            {
                Available = false;
                Debug.LogWarning("PulseDesk: speech recognition check failed. " + e.Message);
            }
            Debug.Log("PulseDesk: speech recognition available = " + Available);
        }

        private void StartRecognizer()
        {
            Listening = true;
            _deadline = Time.unscaledTime + ListenLimitSeconds;
            AndroidApplication.InvokeOnUIThread(() =>
            {
                try
                {
                    AndroidJNI.AttachCurrentThread();
                    if (_recognizer == null)
                    {
                        using (AndroidJavaClass sr = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                            _recognizer = sr.CallStatic<AndroidJavaObject>("createSpeechRecognizer", _activity);
                        _listener = new Listener(this);
                        _recognizer.Call("setRecognitionListener", _listener);
                    }
                    using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.speech.action.RECOGNIZE_SPEECH"))
                    {
                        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form").Dispose();
                        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.PARTIAL_RESULTS", true).Dispose();
                        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.MAX_RESULTS", 1).Dispose();
                        _recognizer.Call("startListening", intent);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("PulseDesk: speech could not start. " + e.Message);
                    Post(() => Fail("Speech could not start. Type instead"));
                }
            });
        }

        private void CallOnUi(string method)
        {
            AndroidJavaObject recognizer = _recognizer;
            if (recognizer == null) return;
            AndroidApplication.InvokeOnUIThread(() =>
            {
                try { AndroidJNI.AttachCurrentThread(); recognizer.Call(method); }
                catch (Exception e) { Debug.LogWarning("PulseDesk: speech " + method + " failed. " + e.Message); }
            });
        }

        private void OnDestroy()
        {
            CallOnUi("destroy");
        }

        // Called from the Android UI thread: everything is posted to Unity's main thread.
        private void OnResults(string text, bool final)
        {
            Post(() =>
            {
                if (!Listening) return;
                if (!final)
                {
                    if (text.Length > 0 && _partial != null) _partial(text);
                    return;
                }
                Listening = false;
                if (text.Length > 0) { if (_final != null) _final(text); }
                else Fail("Did not catch that. Pinch Speak and try again");
            });
        }

        private void OnError(int code)
        {
            Post(() =>
            {
                if (!Listening) return;
                Debug.Log("PulseDesk: speech error " + code);
                switch (code)
                {
                    case 6:
                    case 7: Fail("Did not catch that. Pinch Speak and try again"); break;
                    case 2:
                    case 1: Fail("Speech needs the internet right now. Type instead"); break;
                    case 9: Fail("The microphone is off for PulseDesk. Type instead"); break;
                    default: Fail("Speech is not available right now. Type instead"); break;
                }
            });
        }

        private class Listener : AndroidJavaProxy
        {
            private readonly SpeechInput _owner;

            public Listener(SpeechInput owner) : base("android.speech.RecognitionListener")
            {
                _owner = owner;
            }

            public override AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] javaArgs)
            {
                switch (methodName)
                {
                    case "onResults": _owner.OnResults(Read(javaArgs[0]), true); return null;
                    case "onPartialResults": _owner.OnResults(Read(javaArgs[0]), false); return null;
                    case "onError": _owner.OnError(javaArgs[0].Call<int>("intValue")); return null;
                    case "onReadyForSpeech":
                    case "onBeginningOfSpeech":
                    case "onRmsChanged":
                    case "onBufferReceived":
                    case "onEndOfSpeech":
                    case "onEvent":
                        return null;
                }
                return base.Invoke(methodName, javaArgs);
            }

            private static string Read(AndroidJavaObject bundle)
            {
                if (bundle == null) return "";
                using (AndroidJavaObject list = bundle.Call<AndroidJavaObject>("getStringArrayList", "results_recognition"))
                {
                    if (list == null || list.Call<int>("size") == 0) return "";
                    using (AndroidJavaObject first = list.Call<AndroidJavaObject>("get", 0))
                        return first == null ? "" : first.Call<string>("toString").Trim();
                }
            }
        }
#endif
    }
}
