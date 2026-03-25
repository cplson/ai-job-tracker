using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace JobTracker.API.Helpers
{
    public static class FileHelper
    {
        private static readonly string UploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        // Ensure folder exists
        static FileHelper()
        {
            if (!Directory.Exists(UploadsFolder))
                Directory.CreateDirectory(UploadsFolder);
        }

        /// <summary>
        /// Saves an uploaded file and returns the absolute path.
        /// </summary>
        public static async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(UploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // optional: log exception
                }
            }
        }

        /// <summary>
        /// Returns just the file name from a path
        /// </summary>
        public static string GetFileName(string filePath) => Path.GetFileName(filePath);
    }
}