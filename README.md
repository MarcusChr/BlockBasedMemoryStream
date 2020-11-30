# BlockBasedMemoryStream
##High-speed FIFO Memory Stream based on fixed-sized memory blocks linked together.

###**Changelog:**
v1.3.0:
	- Added support for block-pooling, which will reuse blocks with already allocated memory, resulting in increased performance.
    - Fixed bugs.
    - Increased performance through fixing faulty logic.

v1.2.2:
    - Fixed a bug, which would cause the cached length to be less than 0, when reading the stream to the end.

v1.2.1:
    - Fixed a bug where calling the Clear-method would enable LengthCaching, even if it was disabled.

v1.2.0:
    - Added support for Stream.CopyTo(Stream, int).

v1.0.1:
    - Added optional feature: caching of length.