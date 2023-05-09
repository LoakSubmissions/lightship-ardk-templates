# Shared AR Template

A simple template scene that provides useful tools for creating multiplayer meshing based experiences.

## Table of Contents
- [Included Tools and Features](#included-tools-and-features)
- [In The Scene](#in-the-scene)
- [FAQ](#faq)

## Included Tools and Features

These are the Loak tools and Lightship ARDK features that we set up for you in this scene template:

* **Meshing** - An ARDK feature that maps out the users environment as a mesh in real time.
* **Loak Scanner** - A smooth meshing setup phase for use with any real-time meshing experience.
* **Loak Room Management** - A simple interface for hosting and joining multiplayer sessions.
* **Loak Session Manager** - A wrapper around Lightship's framework that provides simpler, more intuitive control.

## In The Scene

* **ARSceneCamera** - Manages the AR camera. Do not remove unless you are experienced with Lightship.
* **Directional Light** - Default scene lighting for visibility. Modify as needed.
* **Cube** - A test object to ensure correct scene configuration. It should disappear upon localization complete.
* **Lightship Scripts** - This object is provided to hold ARDK scripts. It currently contains the ARMeshManager, CapabilityChecker, and AndroidPermissionRequester scripts.
* **Loak Scanner** - Contains logic and UI for the Loak Scanner tool. Remove if not using the scanner.
* **EventSystem** - Handles UI interaction. Required for clickable UI elements.
* **Loak Multiplayer** - Contains session management and room managment scripts and UI. Do not remove unless you are experienced with Lightship.
* **Loak Example** - Containts simple scripts to demonstrate how to use the session manager to place objects in multiplayer. Modify as you wish.

## FAQ

### What can I do with this template?

Anything! Shared AR is a very powerful tool that can be used in any way you see fit. If you need any ideas or want to get more familiar with how the shared AR can be used, check out the [Lightship wesbite](https://lightship.dev/). You can also check out the experiences hosted on our platform to see what us and other people have made with these tools: [Our Website](https://www.loak.co/)

### What is meshing?

Meshing is an ARDK feature that maps the real world environment as a standard mesh for use within your experience. You can read more about how it works or how to use it on the [Lightship official meshing guide.](https://lightship.dev/guides/meshing/)

### How do I use the Loak Scanner?

The Loak Scanner script is a Singleton, so you can access all of its public functions and variables with `LoakScanner.Instance`.

By default it's configured to wait for at least 20 mesh blocks to generate before completing.

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

### How do I use the Loak Room Management?

Loak Room Managment is designed as a drag and drop solution that works out of the box. It is a Singleton, so you can access all of its public functions and variables with `LoakRoomManagement.Instance`.

By default it's configured to start on scene load and provides options for playing solo or multiplayer.

**Settings:**
* `string roomPrefix`: Text appended to the room code (abstracted from user). Should be set to the name of your game.
* `int roomCap`: Max number of users per room. Lightship has problems with more than 5.
* `string username`: Name of the local player shown to others. Must be set at runtime.

**Methods:**
* `GenerateRoomCode()`: Generates a random alphanumeric 6 digit code.
* `SetRoomCode(string code)`: Sets the room code.
    * `string code`: Code to be set to.
* `PlaySolo()`: Starts a single player AR session.
* `CreateRoom()`: Attempts to create and join a new room with a random room code.
* `SubmitCode()`: Attempts to join an existing room with stored room code.
* `StartRoom()`: Starts localization and AR for all joined peers. Can only be used by the Host.

### How do I use the Loak Session Manager?

Loak Session Manager is a Singleton, so you can access all of its public functions and variables with `LoakRoomManagement.Instance`.

By default in this template it's configured to work with Loak Room Management.

**Settings:**
* `bool arOnStart`: If true, starts a simple AR session on Start.

**Methods:**
* `JoinSession(string sessionIdentifier)`: Joins a multiplayer networking session using sessionIdentifier.
    * `string sessionIdentifier`: A string used to match players together into a session.
* `LeaveSession()`: Leaves the current multiplayer session.
* `StartSoloSession()`: Starts a normal AR session without localization or networking.
* `StartMultiplayerSession()`: Starts a multiplayer AR session and begins localization.
* `SendToHost(uint tag, object[] objs, TransportType tt)`: Sends a list of serializable objects to the session host.
    * `uint tag`: An integer representing the content to expect. Set this to a known value so you know how to use this data when it arrives.
    * `objectp[] objs`: An array of serializable objects to be sent. Serializable types include most primitives, arrays, Guids, strings, and Vectors.
    * `TransportType tt`: How the message should be sent. Use Unreliable for frequent updates (like position) and Reliable for infrequent essential communication.
* `SendToAll(uint tag, Guid origin, object[] objs, TransportType tt)`: Sends a list of serializable objects to all connected peers. Only usable by Host.
    * `uint tag`: An integer representing the content to expect. Set this to a known value so you know how to use this data when it arrives.
    * `Guid origin`: The Guid of the original sender of the data.
    * `objectp[] objs`: An array of serializable objects to be sent. Serializable types include most primitives, arrays, Guids, strings, and Vectors.
    * `TransportType tt`: How the message should be sent. Use Unreliable for frequent updates (like position) and Reliable for infrequent essential communication.
* `SendToPeer(uint tag, IPeer target, object[] objs, TransportType tt)`: Sends a list of serializable objects to a specific peer. Only usable by Host.
    * `uint tag`: An integer representing the content to expect. Set this to a known value so you know how to use this data when it arrives.
    * `IPeer target`: The peer object representing the user to send the message to.
    * `objectp[] objs`: An array of serializable objects to be sent. Serializable types include most primitives, arrays, Guids, strings, and Vectors.
    * `TransportType tt`: How the message should be sent. Use Unreliable for frequent updates (like position) and Reliable for infrequent essential communication.

**Events:**
* `OnSessionJoined`: Triggered when a multiplayer networking session is connected to.
* `OnSessionStarted`: Invoked when the AR session is started.
* `OnSessionLocalized`: Invoked when the multiplayer AR session finishes localizing.
* `OnPeerJoined`: Invoked when a user joins the multiplayer networking session.
* `OnPeerLeft`: Invoked when a user leaves the multiplayer networking session.
* `OnDataRecieved`: Invoked when we recieve data from a peer.
    * `uint`: Integer representing the content to expect.
    * `Guid`: The id of the person who sent the data.
    * `object[]`: The array of objects received. You must cast the items in this array to the data type they were when they were sent. (You should know what types based on the uint tag)

### I have questions that aren't answered here.

If you still need help, feel free to reach out to us through our [Discord server](https://discord.gg/y8wzR8MKKk). If your problem is with a Lightship feature and not the Loak tools provided in this template, you may get better results by asking in [Lightship's forum](https://community.lightship.dev/) or [Discord server](https://discord.gg/RM6m4nWmYp).
