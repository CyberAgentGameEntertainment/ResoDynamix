<p align="center">
<img src="Documentation/rezo_dmx_col_wh.png#gh-light-mode-only" alt="ResoDynamix">
<img src="Documentation/rezo_dmx_col_bk.png#gh-dark-mode-only" alt="ResoDynamix">
</p>

# ResoDynamix

[![license](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![license](https://img.shields.io/badge/PR-welcome-green.svg)](https://github.com/CyberAgentGameEntertainment/ResoDynamix/pulls)
[![license](https://img.shields.io/badge/Unity-2022.0-green.svg)](#Requirements)

[Documentation (日本語)](README_JA.md)<br/>

## 1. Overview
ResoDynamix is a dynamic resolution change library that allows you to change the rendering resolution of base cameras and overlay cameras.<br/>
While uGUI-based UI rendering can be rendered at a different resolution from the base camera by specifying Overlay as the Render Mode, this library allows you to render at a different resolution from the base camera even when the Render Mode is not Overlay.<br/>

If you are using Unity 2022 or Unity 6 (Compatible mode), please use the code from the `compatible-mode` branch. For details, refer to the document below.

[Documentation (Unity 2022 or Compatible)](README_2022_or_Compatible.md)<br/>

## 2. How To Use

### 2.1 Installation to Project
Installation is performed with the following steps.

1. Select Window > Package Manager
2. Select "+" button > Add package from git URL
3. Enter the following to install
   * https://github.com/CyberAgentGameEntertainment/ResoDynamix.git?path=/Assets/ResoDynamix

<p align="center">
  <img width="60%" src="https://user-images.githubusercontent.com/47441314/143533003-177a51fc-3d11-4784-b9d2-d343cc622841.png" alt="Package Manager">
</p>

Alternatively, open Packages/manifest.json and add the following to the dependencies block.

```json
{
    "dependencies": {
        "jp.co.cyberagent.reso-dynamix": "https://github.com/CyberAgentGameEntertainment/ResoDynamix.git?path=/Assets/ResoDynamix"
    }
}
```

To specify a version, write as follows.

* https://github.com/CyberAgentGameEntertainment/ResoDynamix.git?path=/Assets/ResoDynamix#1.0.0

Note that if you see a message like `No 'git' executable was found. Please install Git on your system and restart Unity`, you need to set up Git on your machine.

To update the version, rewrite the version using the above procedure.  
If you don't specify a version, you can update by opening the package-lock.json file and rewriting the hash for this library.

```json
{
  "dependencies": {
      "jp.co.cyberagent.reso-dynamix": {
      "version": "git+ssh://git@github.com:CyberAgentGameEntertainment/ResoDynamix.git?path=/Assets/ResoDynamix",
      "depth": 0,
      "source": "git",
      "dependencies": {},
      "hash": "..."
    }
  }
}
```

### 2.2 Add Create Dynamix Resolution Image Feature to Universal Renderer Data
Add ```Create Dynamix Resolution Image Feature``` to the Universal Renderer Data used for scene rendering.<br/>
<img src="Documentation/011.png" alt="Add Create Dynamix Resolution Image Feature">

### 2.3 Add ResoDynamix
Add the ResoDynamix component to the scene. Note that this component can only be placed once per scene.<br/>
<img src="Documentation/001.png" alt="ResoDynamix component">

### 2.4 Add ResoDynamixController
Add ResoDynamixController to the scene. This component can be placed multiple times in the scene.<br/>
<img src="Documentation/014.png" alt="ResoDynamixController component">


### 2.5 Specify Base Camera to Change Resolution
Specify the base camera whose resolution you want to change in the BaseCamera field of ResoDynamixController.<br/>

<img src="Documentation/015.png" alt="Specify base camera">


### 2.6 Add ResoDynamixController to ResoDynamix
Add the ResoDynamixController placed in step 2.4 to the Controllers field of the ResoDynamix placed in step 2.3.<br/>

<img src="Documentation/006.png" alt="Add ResoDynamixController">

## 3. Reso Dynamix Component Parameters
<img src="Documentation/012.png" alt="Reso Dynamix component">

| Property Name | Description |
| ---- | ---- |
|Controllers| Reso Dynamix Controllers placed in the scene.<br/>All controllers placed in the scene must be registered here.|

## 4. Reso Dynamix Controller Component Parameters
<img src="Documentation/014.png" alt="Reso Dynamix Controller component">

| Property Name | Description |
| ---- | ---- |
| Base Camera Render Scale | Rendering scale of the Base Camera.<br/>You can change the rendering resolution of the Base Camera by changing this value. When the Render Scale is 1.0, Reso Dynamix processing is skipped. |
| Result Render Scale | Rendering scale of the final hybrid image.<br/>You can also change the UI resolution by changing this resolution secondarily, so specify 1 or less when you want to reduce UI rendering load.<br/> However, when this render scale is set to 1 or less, intermediate textures are required, which increases memory usage accordingly.|
| Base Camera | Base Camera |
| Use Depth Texture With Overlay Camera| Checkbox for whether to use depth texture in overlay camera rendering. <br/> Check this if you want to use depth testing or stencil masks in the overlay camera.|