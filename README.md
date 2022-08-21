# ConsoleGameEngine
This is a C# port of the Console Game Engine and the First Person Shooter that javidx9 on youtube did https://www.youtube.com/c/javidx9.

This was mostly a first outing for myself into C# and programming as a whole. A lot of the code could probably be optimized.
A few changes that I made were the ability to use PNGs as textures and the ability to render the console borderless. The textures can be any size and will sample down to fit the "geometry" in the engine.

Currently only runs on Windows due to the win32API calls. I attempted to do the rendering with ANSI escape sequences in order to support multi-platform, but the performance of writing to the Console's buffer with just C#'s Console class is not fast enough to have a reasonable framerate. If anyone sees this and knows of a way to write to the console faster with ANSI codes, then let me know.

No license.
