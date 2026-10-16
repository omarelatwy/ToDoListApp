using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ToDoListApp.Models
{
    public class TodoItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "يرجى تحديد تاريخ الانتهاء")]
        [Display(Name = "Deadline")]
        public DateTime Deadline { get; set; }

        public string? FilePath { get; set; }
    }

    public class TodoItemCreateViewModel
    {
        [Required(ErrorMessage = "العنوان مطلوب")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "يرجى تحديد تاريخ الانتهاء")]
        public DateTime Deadline { get; set; }

        [Required(ErrorMessage = "الرجاء رفع ملف مرفق")]
        public IFormFile File { get; set; } = null!;
    }

    public class TodoItemEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "يرجى تحديد تاريخ الانتهاء")]
        public DateTime Deadline { get; set; }

        public string? ExistingFilePath { get; set; }

        public IFormFile? NewFile { get; set; }
    }
}
