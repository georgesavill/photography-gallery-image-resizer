# photography-gallery-image-resizer

Image resizing application to generate thumbnail (315px wide) and preview (765px wide) images for the [https://github.com/georgesavill/photography-gallery](photography-gallery) website project.

As a console application, it takes two arguments: input directory, output directory.

Files moved from the input directory to the output directory in addition to the resized images, and empty directories from the input directory are removed.

To run as a docker container:

```
docker build -t photo-resizer .
```
```
docker run -v [INPUT DIRECTORY]:/data/input -v [OUTPUT DIRECTORY]:/data/output photo-resizer
```