# Bro Cam — Windows virtual-camera MVP

This repository folder builds a Windows 11 virtual-camera installer using a Media Foundation virtual-camera media source.

## What this first build does
- Installs the **Bro Cam Virtual Camera** media source into Windows.
- Registers it so camera-aware apps can enumerate it.
- Starts a controller app that creates the camera for the current Windows session.
- Removes the session camera when the controller exits.

## Current limitation
This is the first virtual-camera foundation build. The upstream Media Foundation sample currently generates its own sample frames; it is **not yet connected to a scene compositor**. Webcam/image/text layers, draggable source editing, and sending the composed preview to the camera are not implemented in this MVP. Do not mistake this for the finished OBS-style editor.

## Requirements
- Windows 11 (build 22621 or later recommended)
- x64 Windows
- Administrator approval during installation (the Media Foundation COM media source is registered machine-wide)

## Build
Push a change under `BroCam/` or manually run the **Build Bro Cam for Windows** workflow in GitHub Actions. The workflow builds on a hosted Windows runner and uploads `BroCam-Setup.exe` as the `BroCam-Windows-Installer` artifact.

## Third-party source
The virtual-camera media source is built from [smourier/VCamNetSample](https://github.com/smourier/VCamNetSample), copyright Simon Mourier, under the MIT License. The license is included in the installed application as `THIRD-PARTY-LICENSE.txt`.

The sample uses Windows 11 Media Foundation's `MFCreateVirtualCamera` API. It is not a kernel-mode webcam driver.
