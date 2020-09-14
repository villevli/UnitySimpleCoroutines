# Unity Simple Coroutines
An alternative to Unity's Coroutines with no delay when yielding

This has most of the features that Unity's coroutines have but unlike normal coroutines it does not have any frame delays when you yield return a nested IEnumerator.

Unfortunately this does not yet support Unity's yield instructions that do not inherit from IEnumerator like WaitForSeconds, WaitForEndOfFrame and AsyncOperation.

## Installation
- Copy the contents of this repository or only the *Scripts* folder to your *Assets* folder. Preferably place it in a folder called *SimpleCoroutines*

## How To
Create a `SCoroutineRunner` instance and call the following functions on it:
- `StartCoroutine(IEnumerator ie)`: Start executing the given `IEnumerator` like a normal coroutine. This will execute the coroutine and nested IEnumerators until it ends or yield returns `null`
- `Update()`: Continue executing each started coroutine and nested IEnumerators until it ends or yield returns `null`. Usually you want to call this once per frame

Yield return any of the following in the `IEnumerator`:
- `yield return null`: Code after the yield will resume in the next frame
- `yield break`: Stop execution of current method. This will never cause the calling method to wait an extra frame unlike when this is used in normal coroutines
- Any `IEnumerator`: Can be used to execute a nested function. Wait until `MoveNext()` returns false
- Any `CustomYieldInstruction`: Works just like in normal coroutines. For example `new WaitForSecondsRealtime(float time)` or `new WaitUntil(Func<bool> predicate)`
- `new SWaitForSeconds(float time)`: Wait for seconds using scaled time. You must use this instead of Unity's `new WaitForSeconds(float seconds)`. See [Current Limitations](#current-limitations)
- Yield returning anything else will just make the method wait a frame like `yield return null`

## Example Code
```cs
using System.Collections;
using UnityEngine;
using SimpleCoroutines;

public class MyMonoBehaviour : MonoBehaviour
{
    private SCoroutineRunner _runner = new SCoroutineRunner();

    private void Start()
    {
        _runner.StartCoroutine(MyRoutine());
    }

    private void Update()
    {
        _runner.Update();
    }

    private IEnumerator MyRoutine()
    {
        yield return MyNestedRoutine();
        // Here we are still in the same frame unlike when using normal coroutines

        yield return null;
        // Now it's the next frame

        yield return new SWaitForSeconds(2.5f);
        // Now 2.5 seconds have elapsed in game time
    }

    private IEnumerator MyNestedRoutine()
    {
        // Return without waiting to next frame
        yield break;
    }
}
```

## Current Limitations
- SCoroutine does not support Unity's `YieldInstruction`s that do not inherit from `IEnumerator`, like `WaitForSeconds`, `WaitForEndOfFrame` and `AsyncOperation`. This means that yield returning for example `UnityWebRequest.SendWebRequest()` or `SceneManager.LoadSceneAsync()` will not actually wait for it to complete
- There is no `StopCoroutine()` function
- You have to declare `SCoroutineRunner` and call `Update()` on it which might add clutter to your code

## TODO
- Support Unity's `YieldInstruction`s in SCoroutines
- Add `StopCoroutine()` function
- Add an extension method to `MonoBehaviour` so you can use SCoroutines without `SCoroutineRunner` like normal coroutines just by calling `this.StartSCoroutine(IEnumerator ie)` in your MonoBehaviour script
