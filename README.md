# Loak Lightship ARDK Template

A simple template project that provides useful tools for setting up a project with [Niantic's Lightship ARDK](https://lightship.dev/)

## Table of Contents

1. [Getting Started](#getting-started)
2. [Setup](#setup)
   - [Authenticating Lightship ARDK](#authenticating-lightship-ardk)
   - [Choosing a Scene Template](#choosing-a-scene-template)
3. [Building](#building)
   - [For iOS](#for-ios)
   - [For Android](#for-android)
4. [FAQ](#faq)
5. [Features](#features)
   - [Available](#available)
   - [Roadmap](#roadmap)

## Getting Started
First, ensure you have the following installed:
- [Unity Hub](https://unity.com/download): latest
- [Unity Editor](https://unity.com/releases/editor/archive): 2021.3.9f1+
- [Gradle](https://gradle.org/releases/): v6.9.4+ (Android Only)

Next, clone the repository wherever you keep your code:
```
git clone https://github.com/LoakSubmissions/lightship-ardk-template.git
```

## Setup
### Authenticating Lightship ARDK
1. Create a Lightship account [here](https://lightship.dev/signin)
2. In the Lighship developer dashboard, create a new project, and copy the API key
3. Then, in the Unity project view, go to `Assets > Resources > ARDK` and select the `ARDKAuthConfig`
4. Paste your API key in the `Api Key` field

If you get lost, reference the [official Lightship ARDK authentication guide](https://lightship.dev/docs/ardk/ardk_fundamentals/authentication.html#doxid-authentication)

### Choosing a scene template
Scene templates make it easy to get building with Lightship ARDK
1. In the Unity project, navigate to `Assets > Loak > Scene Templates`
2. Browse through the different scenes and choose one that works for you!

For information about the scene templates we provide, check them out at the links below:
* [Empty Template](Assets/Loak/Scene%20Templates/Empty%20Template.md)
* [Meshing Template](Assets/Loak/Scene%20Templates/Meshing%20Template.md)
* [Shared AR Template](Assets/Loak/Scene%20Templates/Shared%20AR%20Template.md)
* [Loak Essentials](Assets/Loak/Scene%20Templates/Loak%20Essentials.md)

## Building
### For iOS
- If not already set to the iOS platform, please select `iOS` and then click `Switch Platform`
- Click `Build`

### For Android
- Go to `Edit > Preferences > External Tools` and scroll to the bottom.
- Ensure that "Gradle installed with Unity" is unchecked and the path is pointing to your Gradle v6.9.4+ install
- Navigate to `File > Build Settings`
- If not already set to the Android platform, please select `Android` and then click `Switch Platform`
- Click `Build`

## FAQ

### What can I do with this template?

Anything! The scene included is a barebones ARDK setup that can be used in any way you see fit. If you need any ideas or want to get more familiar with what the ARDK provides, check out the [Lightship wesbite](https://lightship.dev/). You can also check out the experiences hosted on [our platform](https://www.loak.co/) to see what us and other people have made with these tools.

### I have questions that aren't answered here.

If you still need help, feel free to reach out to us through our [Discord server](https://discord.com/invite/bWfpMfH3BK). If your problem is with a Lightship feature and not the Loak tools provided in this template, you may get better results by asking in [Lightship's forum](https://community.lightship.dev/) or [Discord server](https://discord.gg/RM6m4nWmYp).

## Features

### Available

These are the Loak features and tools that we currently provide in this template:

* **Loak Scanner** - A smooth meshing setup phase for use with any real-time meshing experience.
* **Loak Leaderboards** - A simple score submission and leaderboard display system for games that want a more competitive feel.

### Roadmap

These are some of the Loak features and tools that we plan to add to this template in the future. Feel free to suggest some in our [Discord server!](https://discord.com/invite/bWfpMfH3BK)

* **Loak Leaderboards Update** - More flexibility for the leaderboard tool when used outside of the Loak platform.
* **Mesh Object Placement** - Easy to use solutions for placing objects in the real world.
* **Shared AR Room Managment** - A simlified interface for connecting users in shared AR experiences.
* **Loak Pause Menu** - Super simple and modular pause menu that will make publishing to Loak even easier than before.
