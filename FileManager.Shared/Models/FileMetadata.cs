using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Shared.Models
{
    [Table("file_metadata")]
    public class FileMetadata
    {
        [Key]
        [Column("file_id")]
        public long FileId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("type")]
        public string Type { get; set; }

        [Column("size")]
        public long Size { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }

        [Column("modified_date")]
        public DateTime ModifiedDate { get; set; }

        [Column("uploader_id")]
        public long UploaderId { get; set; }

        [MaxLength(100)]
        [Column("uploader_name")]
        public string UploaderName { get; set; }

        [Column("editor_id")]
        public long EditorId { get; set; }

        [MaxLength(100)]
        [Column("editor_name")]
        public string EditorName { get; set; }
    }
}