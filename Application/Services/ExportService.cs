using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Infrastructure.Data;

namespace WorkManagementSystem.Application.Services
{
    public class ExportService : IExportService
    {
        private readonly AppDbContext _context;

        public ExportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportTasksToExcel()
        {
            var tasks = await _context.Tasks.ToListAsync();
            var taskAssignees = await _context.TaskAssignees.ToListAsync();
            var units = await _context.Units.ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Danh sách công việc");

            // Header
            sheet.Cell(1, 1).Value = "STT";
            sheet.Cell(1, 2).Value = "Tên công việc";
            sheet.Cell(1, 3).Value = "Mô tả";
            sheet.Cell(1, 4).Value = "Deadline";
            sheet.Cell(1, 5).Value = "Phòng ban";

            // Style header
            var headerRow = sheet.Range("A1:E1");
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data
            int row = 2;
            int stt = 1;
            foreach (var task in tasks)
            {
                var unitNames = taskAssignees
                    .Where(ta => ta.TaskId == task.Id && ta.UnitId.HasValue)
                    .Select(ta => units.FirstOrDefault(u => u.Id == ta.UnitId)?.Name ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();

                sheet.Cell(row, 1).Value = stt++;
                sheet.Cell(row, 2).Value = task.Title;
                sheet.Cell(row, 3).Value = task.Description ?? "";
                sheet.Cell(row, 4).Value = task.DueDate.HasValue
                    ? task.DueDate.Value.ToString("dd/MM/yyyy")
                    : "Không có";
                sheet.Cell(row, 5).Value = string.Join(", ", unitNames);

                if (row % 2 == 0)
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportProgressToExcel()
        {
            var progresses = await _context.Progresses.ToListAsync();
            var tasks = await _context.Tasks.ToListAsync();
            var users = await _context.Users.ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Tiến độ công việc");

            // Header
            sheet.Cell(1, 1).Value = "STT";
            sheet.Cell(1, 2).Value = "Công việc";
            sheet.Cell(1, 3).Value = "Nhân viên";
            sheet.Cell(1, 4).Value = "Mô tả tiến độ";
            sheet.Cell(1, 5).Value = "Phần trăm";
            sheet.Cell(1, 6).Value = "Trạng thái";
            sheet.Cell(1, 7).Value = "Ngày cập nhật";

            // Style header
            var headerRow = sheet.Range("A1:G1");
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data
            int row = 2;
            int stt = 1;
            foreach (var p in progresses)
            {
                var taskTitle = tasks.FirstOrDefault(t => t.Id == p.TaskId)?.Title ?? "";
                var userName = users.FirstOrDefault(u => u.Id == p.UserId)?.FullName ?? "";

                var statusText = p.Status.ToString() switch
                {
                    "NotStarted" => "Chưa bắt đầu",
                    "InProgress" => "Đang thực hiện",
                    "Submitted" => "Chờ duyệt",
                    "Approved" => "Đã phê duyệt",
                    "Rejected" => "Bị từ chối",
                    _ => p.Status.ToString()
                };

                sheet.Cell(row, 1).Value = stt++;
                sheet.Cell(row, 2).Value = taskTitle;
                sheet.Cell(row, 3).Value = userName;
                sheet.Cell(row, 4).Value = p.Description ?? "";
                sheet.Cell(row, 5).Value = p.Percent + "%";
                sheet.Cell(row, 6).Value = statusText;
                sheet.Cell(row, 7).Value = p.UpdatedAt.ToString("dd/MM/yyyy HH:mm");

                if (row % 2 == 0)
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}