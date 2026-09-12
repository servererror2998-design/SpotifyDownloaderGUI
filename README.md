# SpotifyDownloaderGUI

Windows GUI front-end for a user-supplied Spotify downloader backend.

## Scope

This repository provides a native Windows GUI shell for a locally installed backend. It does not implement DRM bypass, credential theft, or extraction of protected Spotify streams.

## Build

The GitHub Actions workflow builds a self-contained .NET Windows release and packages it as a ZIP artifact.

## Run

Launch `SpotifyDownloaderGUI.exe`, paste a supported URL, select an output directory and format, then start the operation.

## Backend

The GUI expects a user-configured backend executable. See `backend/example-backend.ps1` for the documented process contract.
