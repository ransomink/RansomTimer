<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/ransomink/timer">
<!--     <img src="https://i.imgur.com/8nYRixj.png" alt="Logo" width="80" height="80"> -->
  </a>

<h3 align="center">Custom Unity Timer</h3>

  <p align="center">
    A custom timer for invoking actions and events after a given delay.
    <br />
    <a href="https://github.com/ransomink/timer"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/ransomink/timer">View Demo</a>
    ·
    <a href="https://github.com/ransomink/timer/issues">Report Bug</a>
    ·
    <a href="https://github.com/ransomink/timer/issues">Request Feature</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#dependencies">Dependencies</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a>
      <ul>
        <li><a href="#features">Features</a></li>
      </ul>
    </li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

<p align="center"><img src="https://i.imgur.com/8nYRixj.png" alt="Package-CustomUnityTimer-Logo" title="Custom Unity Timer Logo"></p>

At some point in time during your "game dev" adventure, you'll need to wait *X* amount of time to do *Y*. When I first explored this concept I was ushered towards [Coroutines](https://docs.unity3d.com/Manual/Coroutines.html): they work great, simple to execute, but caused an issue with memory (at the time); the ever popular tween engine: DOTween, LeanTween, etc.; or just using a variable to track the time in Update (albeit, with constant checks). Each had their own strengths and weakness, but I decided to expand on the latter as I could add any functionality needed without adhering to a concrete system. This evolved into a custom unity timer. It was made to simplify tracking time and adding to a project without injecting itself into its codebase.

<p align="right">(<a href="#top">back to top</a>)</p>



### Built With

* [C#](https://docs.microsoft.com/en-us/dotnet/csharp/ "C# Documentation - Get Started, Tutorials, Reference")
* [Unity game engine](https://unity.com/ "Unity Real-Time Development Platform")

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- GETTING STARTED -->
## Getting Started

### Dependencies

* [Ransom Core Framework](https://github.com/ransomink/core-framework)
  ```sh
  https://github.com/ransomink/core-framework.git
  ```

### Installation

1. Open [Package Manager](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@1.8/manual/index.html): **Window > Package Manager** 
3. Click the add button from the status bar. (The options for adding packages appear)

   ![PackageManagerUI-Add](https://i.imgur.com/0lh7t8b.png)

3. Select **Add package from git URL** from the add menu

   ![PackageManagerUI-AddGitURLButton](https://i.imgur.com/fywRugA.png)

4. Enter `https://github.com/ransomink/timer.git` in the text box and click **Add**

   ![PackageManagerUI-AddGitURLButton-Add](https://i.imgur.com/B5E3ajF.png)

5. If installed successfully, the package now appears in the package list with the git tag.

---

<!-- <p>Alternatively, you can follow the guide from Unity Documentation Manual on [Installing from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html).</p> -->

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- USAGE EXAMPLES -->
## Usage
  
Here is the method signature to create a timer:

```cs
/// <summary>
/// Create an Timer instance (active).
/// </summary>
/// <param name="time">The timer duration in seconds.</param>
/// <param name="hasLoop">Does the timer repeat after execution?</param>
/// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
public Timer(float time, bool hasLoop = false, bool isUnscaled = false)
```

There are many overloads to choose from. To create a simple timer, you call:

```cs
new Timer(1f); // One second timer.
```

This will create a one second timer. If you wish to run it continuously, you call:

```cs
new Timer(1f, true);
new Timer(1f, hasLoop: true);
```

This timer will loop endlessly. Timers are affected by [`timeScale`](https://docs.unity3d.com/ScriptReference/Time-timeScale.html); if you're creating slow-motion effects or a game is paused and you wish to play animations, you must set the timer to use [`unscaledTime`](https://docs.unity3d.com/ScriptReference/Time-unscaledTime.html) instead.

```cs
new Timer(1f, isUnscaled: true)
```

But the main use of a timer is to invoke a method after completion. We do so by passing in an action as a parameter.

```cs
Action action = () => Debug.Log("I am ... was ... a timer.");
new Timer(1f, action);
```

This timer will invoke `action` after its completion. You can create or initialize a timer using its constructor, but there is also a convenience method. It contains the same signature (and overloads) as the constructor.

```cs
Timer.Record(1f, action);
```

A timer contains the [`TimerActions`](https://github.com/ransomink/timer/blob/a157aef365dc0acefd479096cf2746e999cabbe5/Scripts/Runtime/Timer.cs#L10) class field which consists of action delegates: `OnComplete`, `OnCancelled`, `OnSuspended`, `OnResumed`, and `OnUpdated`, each invoked when a change of state occurs. You can pass these actions as a parameter.

```cs
TimerActions actions = new TimerActions(
  onComplete:  () => Debug.Log("Timer complete."),
  onCancelled: () => Debug.Log("Timer cancelled."),
  onSuspended: () => Debug.Log("Timer suspended."),
  onResumed:   () => Debug.Log("Timer resumed."),
  onUpdated:   () => Debug.Log("Timer updating...")
);

Timer.Record(1f, actions);
```

Lastly, you can bind a timer to a MonoBehaviour. This ensures the timer only runs if the MonoBehaviour is not destroyed. There is also a convenience method; it contains the same signature (and overloads) as the constructor.

```cs
new  Timer(this, 1f, actions);
Timer.Bind(this, 1f, actions);
```

_For more examples, please refer to the [Documentation](https://example.com)_

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- FEATURES -->
## Features

A timer is created to display a log to the Unity console.

```cs
private Timer timer;

private void Start() {
    timer = Timer.Record(5f, () => Debug.Log("I am ... was ... a timer."));
}
```

The following methods show how to use/manipulate a timer.

---

How to suspend a timer.
```cs
private void Update() {
    if (Input.GetKeyDown(KeyCode.S)) timer.Suspended();
}
```

How to resume a timer.
```cs
private void Update() {
    if (Input.GetKeyDown(KeyCode.R)) timer.Resume();
}
```

How to cancel a timer.
```cs
private void Update() {
    if (Input.GetKeyDown(KeyCode.C)) timer.Cancelled();
}
```

How to set a new duration (effectively resetting it).
```cs
private void Update() {
    if (Input.GetKeyDown(KeyCode.N)) timer.NewDuration(5f);
}
```

How to extend a duration.
```cs
private void Update() {
    if (Input.GetKeyDown(KeyCode.E)) timer.ExtendDuration(5f);
}
```

How to set actions on an existing timer.
```cs
// Create a new TimerActions.
TimerActions actions = new TimerActions(
  onComplete:  () => Debug.Log("Timer complete."),
  onCancelled: () => Debug.Log("Timer cancelled."),
  onSuspended: () => Debug.Log("Timer suspended."),
  onResumed:   () => Debug.Log("Timer resumed."),
  onUpdated:   () => Debug.Log("Timer updating...")
);

// Assign actions to timer.
timer.Actions = actions;

// Overwrite actions on timer.
timer.Actions.Set(
  onComplete:  () => Debug.Log("Timer complete."),
  onCancelled: () => Debug.Log("Timer cancelled.")
);
  
// Edit action on timer.
timer.Actions.OnUpdated = () => Debug.Log("Timer updating...");
```

_For more examples, please refer to the [Documentation](https://example.com)_

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- ROADMAP -->
## Roadmap

- [ ] Add Changelog
- [x] Add an event (action) for each timer state
- [x] Create `TimerActions` class to store events
- [x] Change internal list from `Tuple<Timer, Action>` to `Timer` 
- [x] Make a reusable timer
    - [x] Reset (default values)
    - [x] Reload (Reset ignoring `timerActions` field)
    - [x] Add Set methods (bypass creating new instance)
- [ ] \(Optional) Add object pooling
- [ ] Switch to FSM (to check timer state)

See the [open issues](https://github.com/ransomink/timer/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/amazing-feature`)
3. Commit your Changes (`git commit -m 'Add some amazing-feature'`)
4. Push to the Branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

Joshua Smith <!-- - [@twitter_handle](https://twitter.com/twitter_handle) --> - ransom.ink@gmail.com 

Project Link: [https://github.com/ransomink/timer](https://github.com/ransomink/timer "Custom Unity Timer")

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
* [C# Notes For Professionals](https://goalkicker.com/CSharpBook/CSharpNotesForProfessionals.pdf)
* [Unity C# Reference Source Code](https://github.com/Unity-Technologies/UnityCsReference)
* [Unity Documentation Scripting Reference](https://docs.unity3d.com/ScriptReference/)

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/ransomink/timer.svg?style=for-the-badge
[contributors-url]: https://github.com/ransomink/timer/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/ransomink/timer.svg?style=for-the-badge
[forks-url]: https://github.com/ransomink/timer/network/members
[stars-shield]: https://img.shields.io/github/stars/ransomink/timer.svg?style=for-the-badge
[stars-url]: https://github.com/ransomink/timer/stargazers
[issues-shield]: https://img.shields.io/github/issues/ransomink/timer.svg?style=for-the-badge
[issues-url]: https://github.com/ransomink/timer/issues
[license-shield]: https://img.shields.io/github/license/ransomink/timer.svg?style=for-the-badge
[license-url]: https://github.com/ransomink/timer/blob/master/LICENSE.txt
<!-- [linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555 -->
<!-- [linkedin-url]: https://linkedin.com/in/linkedin_username -->
[product-screenshot]: images/screenshot.png
