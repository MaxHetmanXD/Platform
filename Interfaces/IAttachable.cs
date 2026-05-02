using System.Collections.Generic;
using Platform.Models;

namespace Platform.Interfaces
{
    public interface IAttachable
    {
        List<FileModel> Attachments { get; set; }

        bool HasAttachments { get; }

        void AttachFile(FileModel file);

        bool RemoveFile(FileModel file);

        List<FileModel> GetAttachments();

        bool IsAttachmentAllowed();
    }
}