# Loak Essentials

A simple template scene that provides everything you need to build and publish an experience on the Loak platform.

## Included Tools and Features

These are the Loak tools and Lightship ARDK features that we set up for you in this scene template:

* Real-time Meshing - An ARDK feature that maps out the users environment as a mesh in real time.
* Loak Scanner - A smooth meshing setup phase for use with any real-time meshing experience.
* Loak Leaderboard - An easy to use leaderboard UI element that can be used for both standalone experiences and easily plugs into Loak's leaderboard backend.

## Help

### What can I do with this template?

Anything! This template is specifically designed to make it really easy to develop an experience for the Loak platform, but you're more than welcome to use the tools for your own standalone experience. If you need any ideas or want to get more familiar with how the tools can be used, check out the experiences hosted on [our platform](https://www.loak.co/) to see what us and other people have made with these tools!

### What is realtime meshing?

Realtime meshing is an ARDK feature that maps the real world environment as a standard mesh for use within your experience. You can read more about how it works or how to use it on the [Lightship official meshing guide.](https://lightship.dev/guides/meshing/)

### How do I use the Loak Scanner?

This scene has it set up for you already. The Loak Scanner script is a Singleton, so you can access all of its public functions and variables with `LoakScanner.Instance`.

By default it's configured to start immediately with the scene. If you wish to control it manually, make sure that `autoStart` is toggled off in the inspector. You can then trigger it with `LoakScanner.Instance.StartScan()` and if you need to forcefully end it, the `ForceEndScan(bool immediate)` method will do the trick.

The scanner is intended to be used to set up the mesh before gameplay, so we provide two useful events that you can subscribe to in order to run setup functions or start your experience. These events are `OnScanStart` and `OnScanEnd`.

If you want more or less mesh to be generated before the scan is considered complete, you can adjust the `scanThreshold` variable in the inspector.

### How do I use the Loak Leaderboard?

This scene has it set up for you already. All customizable values are set to the same as they would be on the Loak platform. The Loak Leaderboard script is a Singleton, so you can access all of its public functions and variables with `LoakLeaderboard.Instance`.

You can show and hide the leaderboard with the `LoakLeaderboard.Instance.Show()` and `Hide()` methods respectively.

To populate the leaderboard with your own values, use the `SetFriendEntries(List<(string, long)> entries)` and `SetGlobalEntries(List<(string, long)> entries)` methods. If you're submitting this for publishing on Loak, you don't need to worry about populating it yourself.

The leaderboard will highlight any entries that have the same username as what you set in the `highlightedName` variable. For publishing on Loak, you do not need to set this yourself.

By default, the leaderboard is set to show both the Friends and Global leaderboard tabs. If you're using this for a standalone experience you can change this with the `leaderboardConfiguration` variable in the inspector.

If you want more or less entries to be shown, you can adjust the `numberOfEntries` variable in the inspector.

### I have questions that aren't answered here.

If you still need help, feel free to reach out to us through our [Discord server](https://discord.gg/y8wzR8MKKk). If your problem is with a Lightship feature and not the Loak tools provided in this template, you may get better results by asking in [Lightship's forum](https://community.lightship.dev/) or [Discord server](https://discord.gg/RM6m4nWmYp).
