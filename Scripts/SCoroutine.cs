using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleCoroutines
{
    public class SCoroutine
    {
        private Stack<IEnumerator> ienumeratorStack = new Stack<IEnumerator>(); // The top of the stack is the currently executing IEnumerator
        private SCoroutine nestedCoroutine = null; // If not null, another SCoroutine has been yield returned and is running
        private int yieldFrame = -1; // Wait until Time.frameCount >= yieldFrame

        public bool IsRunning()
        {
            if (yieldFrame > Time.frameCount)
                return true;
            if (ienumeratorStack.Count > 0)
                return true;
            if (nestedCoroutine != null && nestedCoroutine.IsRunning())
                return true;
            return false;
        }

        public void Start(IEnumerator routine)
        {
            ienumeratorStack.Clear();
            ienumeratorStack.Push(routine);
            nestedCoroutine = null;
            yieldFrame = -1;

            Update();
        }

        // Execute a single step of the coroutine. Unity equivalent is the MoveNext you see in coroutine stack traces
        // Returns true if the coroutine should continue running
        public bool Update()
        {
            // This ensures yield return null waits a frame in all situations
            if (yieldFrame > Time.frameCount)
                return true;

            // Do not continue with the IEnumerators until the last returned SCoroutine has finished running
            if (nestedCoroutine != null && nestedCoroutine.IsRunning())
                return true;
            nestedCoroutine = null;

            if (ienumeratorStack.Count > 0)
            {
                var ie = ienumeratorStack.Peek(); // The top of the stack is the currently executing IEnumerator
                if (ie.MoveNext()) // Execute IEnumerator until next yield (true) or break/end (false)
                {
                    // ie.Current is what was yield returned in the IEnumerator
                    object yielded = ie.Current;
                    if (yielded is IEnumerator)
                    {
                        // Move to execute a nested IEnumerator
                        ienumeratorStack.Push(yielded as IEnumerator);
                        return Update(); // Start nested IEnumerator execution without frame delay
                    }
                    else if (yielded is SCoroutine)
                    {
                        // Start waiting on a nested SCoroutine
                        nestedCoroutine = yielded as SCoroutine;
                    }
                    else
                    {
                        // Yielding anything else (like null) causes execution to resume next frame
                        yieldFrame = Time.frameCount + 1;
                    }
                    // TODO: Support Unity's YieldInstruction by starting a real coroutine and yielding it there
                    // TODO: Add logic for custom yieldable objects here
                }
                else
                {
                    ienumeratorStack.Pop();
                    return Update(); // Immediately continue the previous IEnumerator in the stack without frame delay
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // Start and update the coroutines in the player loop
    public class SCoroutineRunner
    {
        private List<SCoroutine> runningCoroutines = new List<SCoroutine>();

        // Call this as you would call MonoBehaviour.StartCoroutine
        public SCoroutine StartCoroutine(IEnumerator ie)
        {
            var sc = new SCoroutine();
            sc.Start(ie);
            runningCoroutines.Add(sc);
            return sc;
        }

        // Call this each frame (e.g. from MonoBehaviour.Update)
        public void Update()
        {
            for (int i = runningCoroutines.Count - 1; i >= 0; i--)
            {
                if (!runningCoroutines[i].Update())
                    runningCoroutines.RemoveAt(i);
            }
        }
    }

    // Works like Unity's WaitForSeconds.
    // This can be used in both SCoroutines and Unity's coroutines because it's based on IEnumerator
    public class SWaitForSeconds : CustomYieldInstruction
    {
        public float waitTime { get; set; }
        float m_WaitUntilTime = -1;

        public override bool keepWaiting
        {
            get
            {
                if (m_WaitUntilTime < 0)
                {
                    m_WaitUntilTime = Time.time + waitTime;
                }

                bool wait = Time.time < m_WaitUntilTime;
                if (!wait)
                {
                    // Reset so it can be reused.
                    m_WaitUntilTime = -1;
                }
                return wait;
            }
        }

        public SWaitForSeconds(float time)
        {
            waitTime = time;
        }
    }
}
