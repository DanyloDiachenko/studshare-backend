using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;

namespace StudShare.Api.Endpoints;

public static class FilesEndpoints
{
    public static void MapFilesEndpoints(this IEndpointRouteBuilder app)
    {
        var files = app.MapGroup("/api/files").WithTags("Files").RequireAuthorization();
        var folders = app.MapGroup("/api/folders").WithTags("Folders").RequireAuthorization();

        files.MapGet("/snapshot", async (CurrentUserService currentUser, FileService fileService) =>
        {
            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var (folderList, fileList) = await fileService.GetSnapshotAsync(user.GroupId.Value);
            return Results.Ok(new { folders = folderList, files = fileList });
        })
        .WithSummary("Files and folders snapshot")
        .WithDescription("Returns all folders and files for the group in a single request. Used for initial tree load.");

        files.MapGet("/search", async (
            string q,
            CurrentUserService currentUser,
            FileService fileService) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new ErrorResponse("Search query is required."));

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var (folderList, fileList) = await fileService.SearchAsync(user.GroupId.Value, q);
            return Results.Ok(new { folders = folderList, files = fileList });
        })
        .WithSummary("Search files and folders")
        .WithDescription("Case-insensitive search by name (`?q=...`). Returns matching files and folders.");

        files.MapPost("/upload", async (
            IFormFile file,
            string? folderId,
            CurrentUserService currentUser,
            FileService fileService) =>
        {
            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var canUpload = user.Role is UserRole.Starosta or UserRole.StudCurator || user.UploadAccess;
            if (!canUpload)
                return Results.Forbid();

            try
            {
                var actor = await currentUser.GetActorAsync();
                var result = await fileService.UploadAsync(user.GroupId.Value, file, folderId, actor);
                return Results.Created($"/api/files/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .DisableAntiforgery()
        .WithSummary("Upload file")
        .WithDescription("Uploads a file to the group storage. Optionally into a folder (`?folderId=`). Requires `UploadAccess`, Starosta, or StudCurator.");

        files.MapGet("/{id}/download", async (Guid id, FileService fileService) =>
        {
            try
            {
                var (path, contentType, fileName) = await fileService.DownloadAsync(id);
                var stream = System.IO.File.OpenRead(path);
                return Results.File(stream, contentType, fileName);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithSummary("Download file")
        .WithDescription("Returns the binary file content with the correct Content-Type and Content-Disposition headers.");

        files.MapPatch("/{id}/rename", async (
            Guid id,
            [FromBody] RenameFileRequest req,
            IValidator<RenameFileRequest> validator,
            CurrentUserService currentUser,
            FileService fileService) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            try
            {
                var updated = await fileService.RenameAsync(id, req, currentUser.UserId, currentUser.UserRole);
                return Results.Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException)        { return Results.NotFound(); }
        })
        .WithSummary("Rename file")
        .WithDescription("The file author, Starosta, or StudCurator can rename a file.");

        files.MapPatch("/{id}/pin", async (
            Guid id,
            CurrentUserService currentUser,
            FileService fileService) =>
        {
            try
            {
                var updated = await fileService.TogglePinAsync(id, currentUser.UserRole);
                return Results.Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException)        { return Results.NotFound(); }
        })
        .WithSummary("Pin / unpin file")
        .WithDescription("Toggles `isPinned`. Starosta or StudCurator only.");

        files.MapDelete("/{id}", async (
            Guid id,
            CurrentUserService currentUser,
            FileService fileService) =>
        {
            try
            {
                var (deletedFileId, storageUsedBytes) =
                    await fileService.DeleteAsync(id, currentUser.UserId, currentUser.UserRole);
                return Results.Ok(new { deletedFileId, storageUsedBytes });
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException)        { return Results.NotFound(); }
        })
        .WithSummary("Delete file")
        .WithDescription("Deletes the file from disk and database. Returns updated `storageUsedBytes`. File author, Starosta, or StudCurator.");

        // --- Folders ---

        folders.MapPost("/", async (
            [FromBody] CreateFolderRequest req,
            IValidator<CreateFolderRequest> validator,
            CurrentUserService currentUser,
            FolderService folderService) =>
        {
            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var canUpload = user.Role is UserRole.Starosta or UserRole.StudCurator || user.UploadAccess;
            if (!canUpload)
                return Results.Forbid();

            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            try
            {
                var actor = await currentUser.GetActorAsync();
                var folder = await folderService.CreateAsync(user.GroupId.Value, req, actor);
                return Results.Created($"/api/folders/{folder.Id}", folder);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new ErrorResponse(ex.Message));
            }
        })
        .WithSummary("Create folder")
        .WithDescription("Creates a new folder in the group. Name must be unique. Requires `UploadAccess`.");

        folders.MapPatch("/{id}/rename", async (
            Guid id,
            [FromBody] RenameFolderRequest req,
            IValidator<RenameFolderRequest> validator,
            CurrentUserService currentUser,
            FolderService folderService) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            try
            {
                var updated = await folderService.RenameAsync(id, req, currentUser.UserId, currentUser.UserRole);
                return Results.Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException)        { return Results.NotFound(); }
        })
        .WithSummary("Rename folder")
        .WithDescription("The folder author, Starosta, or StudCurator can rename a folder.");

        folders.MapPatch("/{id}/pin", async (
            Guid id,
            CurrentUserService currentUser,
            FolderService folderService) =>
        {
            try
            {
                var updated = await folderService.TogglePinAsync(id, currentUser.UserRole);
                return Results.Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException)        { return Results.NotFound(); }
        })
        .WithSummary("Pin / unpin folder")
        .WithDescription("Toggles `isPinned`. Starosta or StudCurator only.");

        folders.MapDelete("/{id}", async (
            Guid id,
            CurrentUserService currentUser,
            FolderService folderService) =>
        {
            try
            {
                var (deletedIds, storageUsedBytes) =
                    await folderService.DeleteAsync(id, currentUser.UserId, currentUser.UserRole);
                return Results.Ok(new { deletedFolderIds = deletedIds, storageUsedBytes });
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException)        { return Results.NotFound(); }
        })
        .WithSummary("Delete folder")
        .WithDescription("Deletes the folder and all nested files. Returns a list of deleted ids and `storageUsedBytes`.");
    }
}
