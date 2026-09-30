# Admin media upload

The Admin movie editor uploads local files directly from the browser to the API. The browser never sends the
original path from the administrator's computer. The API generates a random storage name and returns a logical
identifier for the movie record.

## Local development storage

- Images: `private-media/images`
- Demo MP4 files: `private-media/videos`

Both directories are outside web roots and ignored by Git. Production deployments should configure absolute,
persistent shared paths with `CatalogMedia__ImageRootPath` and `DemoMedia__RootPath`, or replace the local storage
implementation with object storage.

## Endpoints

- `POST /api/admin/media/images`: Admin-only multipart upload, field name `file`; JPEG, PNG or WebP, maximum 10 MB.
- `POST /api/admin/media/videos`: Admin-only multipart upload, field name `file`; MP4, maximum 1 GB.
- `GET|HEAD /api/catalog-images/{key}`: public immutable image content.

Uploaded images produce `/api/catalog-images/{key}`. Uploaded video produces `/videos/{key}.mp4`; that value remains
a private media identifier and cannot be downloaded directly. Playback still requires the profile-scoped ticket flow.

The storage implementation checks declared metadata and file signatures, writes to a temporary file, and moves the
completed file to a random server-controlled name. It never trusts a client path or uses the original filename as a
storage path.

## Transaction boundary

Filesystem upload and the later SQL movie save are separate operations. Cancelling a form after upload can leave an
unreferenced file. Automatic orphan cleanup and deletion of replaced assets remain outside this slice. Existing media
is not deleted when a movie is soft-deleted because restore must keep working.
