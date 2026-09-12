# Backend contract

The GUI launches a user-configured backend executable without opening a console window.

Expected command-line contract:

```text
spotify-dl.exe --url <spotify-url> --format <mp3|flac|ogg|aac> --quality <number> --output <folder>
```

The backend is intentionally not bundled by this repository. The GUI does not implement DRM bypass or protected-stream extraction; use only a backend and account access that you are authorized to use.
