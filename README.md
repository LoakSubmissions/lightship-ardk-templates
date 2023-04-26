# Loak Lightship ARDK Template

A simple template project that provides useful tools for setting up a project with [Niantic's Lightship ARDK](https://lightship.dev/)

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

## Building
### For iOS
- If not already set to the iOS platform, please select `iOS` and then click `Switch Platform`
- Click `Build`

### For Android
- Go to `Edit > Preferences > External Tools` and scroll to the bottom.
- Ensure that "Gradle installed with Unity" is unchecked and the path is pointing to your Gradle v6.9.4+ binaries
- Navigate to `File > Build Settings`
- If not already set to the Android platform, please select `Android` and then click `Switch Platform`
- Click `Build`

## Help

### What can I do with this template?

Anything! The scene included is a barebones ARDK setup that can be used in any way you see fit. If you need any ideas or want to get more familiar with what the ARDK provides, check out the [Lightship wesbite](https://lightship.dev/). You can also check out the experiences hosted on our platform to see what us and other people have made with these tools: [Our Website](https://www.loak.co/)

### How do I use the Loak Scanner?

The "Template (Meshing)" scene has it set up already, but if you wish to use it elsewhere just drag the prefab into your scene. You can find the Loak Scanner prefab by searching in the Project window or at Assets/Loak/Prefabs.

Ensure that you have Lightships ARMeshManager component somewhere in your scene or the scanner will not work. If you don't already have one you can add it to the Loak Scanner object or anywhere else as you see fit. We recommend creating your own object to house your Lightship scripts. Don't forget that your mesh root object needs to have a negative y scale.

After adding the prefab to your scene you can customize its behavior in the inspector. Each value has a tooltip to help you understand what it does. The most important value is the Scan Threshold. This value determines how much mesh needs to be generated before the scan is complete. You will likely need to adjust this value up or down depending on the needs of your experience and your ARMeshManager settings.

### I have questions that aren't answered here.

If you still need help, feel free to reach out to us through our [Discord server](https://discord.gg/y8wzR8MKKk). If your problem is with a Lightship feature and not the Loak tools provided in this template, you may get better results by asking in Lightship's forum or Discord server.

## Features

### Available

These are the Loak features and tools that we currently provide in this template:

* Loak Scanner - A smooth meshing setup phase for use with any real-time meshing experience.

### Roadmap

These are some of the Loak features and tools that we plan to add to this template in the future. Feel free to suggest some in our [Discord server!](https://discord.gg/y8wzR8MKKk)

* Loak Leaderboards - A simple score submission and leaderboard display system for games that want a more competitive feel.
