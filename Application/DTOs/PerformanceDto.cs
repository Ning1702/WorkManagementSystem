namespace WorkManagementSystem.Application.DTOs
{
    public class PerformanceDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;

        // Điểm KPI (0-120, base 100)
        public int Score { get; set; }
        public string Level { get; set; } = string.Empty;       // Xuất sắc / Tốt / Trung bình / Yếu
        public string LevelColor { get; set; } = string.Empty;  // green / blue / yellow / red
        public string LevelIcon { get; set; } = string.Empty;

        // Thống kê công việc
        public int TotalTasks { get; set; }
        public int CompletedOnTime { get; set; }    // Hoàn thành đúng hạn → +5 mỗi cái
        public int CompletedLate { get; set; }      // Hoàn thành nhưng trễ hạn
        public int OverdueTasks { get; set; }       // Quá hạn chưa nộp → phạt lũy tiến
        public int RejectedReports { get; set; }    // Báo cáo bị từ chối → -3 mỗi cái

        // Điểm chi tiết
        public int BonusPoints { get; set; }        // Tổng cộng điểm thưởng
        public int PenaltyPoints { get; set; }      // Tổng điểm bị trừ (Personal)
        public int ReviewPenaltyPoints { get; set; } // Phạt ngâm duyệt (Manager)

        // Phân rã điểm cho Manager
        public bool IsManagerKpi { get; set; }
        public double UnitAverageScore { get; set; }
        public int PersonalScore { get; set; }

        // Cảnh báo
        public bool IsAtRisk { get; set; }          // true nếu vi phạm >= 3 lần hoặc điểm < 60
        public string WarningMessage { get; set; } = string.Empty;
    }
}
