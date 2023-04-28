# Meshing Template

A simple template scene that provides useful tools for creating real-time meshing based experiences.

## Table of Contents
- [Included Tools and Features](#included-tools-and-features)
- [In The Scene](#in-the-scene)
- [FAQ](#faq)

## Included Tools and Features

These are the Loak tools and Lightship ARDK features that we set up for you in this scene template:

* **Real-time Meshing** - An ARDK feature that maps out the users environment as a mesh in real time.
* **Loak Scanner** - A smooth meshing setup phase for use with any real-time meshing experience.

## In The Scene

* **ARSceneManager** - Manages the AR session and camera. Do not remove unless you are experienced with Lightship.
* **Directional Light** - Default scene lighting for visibility. Modify as needed.
* **Cube** - A test object to ensure correct scene configuration. If not visible upon building, your project may be misconfigured.
* **Lightship Scripts** - This object is provided to hold ARDK scripts. It currently only contains the ARMeshManager script.
* **Loak Scanner** - Contains logic and UI for the Loak Scanner tool. Remove if not using the scanner.
* **EventSystem** - Handles UI interaction. Required for clickable UI elements.

## FAQ

### What can I do with this template?

Anything! Meshing is a very powerful tool that can be used in any way you see fit. If you need any ideas or want to get more familiar with how the meshing can be used, check out the [Lightship wesbite](https://lightship.dev/). You can also check out the experiences hosted on our platform to see what us and other people have made with these tools: [Our Website](https://www.loak.co/)

### What is realtime meshing?

Realtime meshing is an ARDK feature that maps the real world environment as a standard mesh for use within your experience. You can read more about how it works or how to use it on the [Lightship official meshing guide.](https://lightship.dev/guides/meshing/)

### How do I use the Loak Scanner?

This scene has it set up for you already. The Loak Scanner script is a Singleton, so you can access all of its public functions and variables with `LoakScanner.Instance`.

By default it's configured to start immediately with the scene and wait for at least 20 mesh blocks to generate before completing.

**Settings:**
* `bool autoStart`: Whether or not the scan should start immediately on scene load.
* `int scanThreshold`: How much mesh (in blocks) should be generated before scan is considered complete.

**Methods:**
* `StartScan()`: Used to manually start or reset scan progress.
* `ForceEndScan(bool immediate)`: Used to manually force the scan to end.
    * `bool immediate`: Whether or not the end delay should be skipped.

**Events:**
* `OnScanStart`: Triggered when the scan begins.
* `OnScanEnd`: Triggered when the scan finishes (whether forced or not).

### I have questions that aren't answered here.

If you still need help, feel free to reach out to us through our [Discord server](https://discord.gg/y8wzR8MKKk). If your problem is with a Lightship feature and not the Loak tools provided in this template, you may get better results by asking in [Lightship's forum](https://community.lightship.dev/) or [Discord server](https://discord.gg/RM6m4nWmYp).
