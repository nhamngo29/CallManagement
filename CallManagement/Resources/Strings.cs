namespace CallManagement.Resources
{
    /// <summary>
    /// Localization-ready string resources.
    /// Replace with actual localization framework (e.g., ResX, i18n) as needed.
    /// </summary>
    public static class Strings
    {
        // ═══════════════════════════════════════════════════════════════════════
        // APP
        // ═══════════════════════════════════════════════════════════════════════
        public static string AppName => "Call Manager";

        // ═══════════════════════════════════════════════════════════════════════
        // TOP BAR
        // ═══════════════════════════════════════════════════════════════════════
        public static string ImportExcel => "Import Excel";
        public static string ExportExcel => "Export Excel";
        public static string ImportTooltip => "Import file Excel chứa danh sách gọi";
        public static string ExportTooltip => "Xuất kết quả cuộc gọi ra file Excel";
        public static string HelpTooltip => "Xem hướng dẫn sử dụng";

        // ═══════════════════════════════════════════════════════════════════════
        // SIDEBAR - STATISTICS
        // ═══════════════════════════════════════════════════════════════════════
        public static string Statistics => "Thống kê";
        public static string Total => "Tổng số";
        public static string QuickGuide => "Hướng dẫn nhanh";

        // ═══════════════════════════════════════════════════════════════════════
        // DATA GRID HEADERS
        // ═══════════════════════════════════════════════════════════════════════
        public static string ColumnId => "#";
        public static string ColumnName => "Tên";
        public static string ColumnPhone => "Số điện thoại";
        public static string ColumnCompany => "Công ty";
        public static string ColumnStatus => "Trạng thái";
        public static string ColumnActions => "Hành động";

        // ═══════════════════════════════════════════════════════════════════════
        // CALL STATUS
        // ═══════════════════════════════════════════════════════════════════════
        public static string StatusNone => "Chưa gọi";
        public static string StatusAnswered => "Nghe máy";
        public static string StatusNoAnswer => "Không nghe";
        public static string StatusInvalid => "Số sai";
        public static string StatusBusy => "Máy bận";

        // ═══════════════════════════════════════════════════════════════════════
        // ACTION TOOLTIPS
        // ═══════════════════════════════════════════════════════════════════════
        public static string ActionAnsweredTooltip => "Đánh dấu: Nghe máy";
        public static string ActionNoAnswerTooltip => "Đánh dấu: Không nghe máy";
        public static string ActionInvalidTooltip => "Đánh dấu: Số sai";
        public static string ActionBusyTooltip => "Đánh dấu: Máy bận";
        public static string ActionResetTooltip => "Đặt lại trạng thái";

        // ═══════════════════════════════════════════════════════════════════════
        // ONBOARDING
        // ═══════════════════════════════════════════════════════════════════════
        public static string OnboardingStep1Title => "Import danh sách";
        public static string OnboardingStep1Description => "Nhấn nút Import Excel để tải lên danh sách số điện thoại cần gọi.";

        public static string OnboardingStep2Title => "Đánh dấu trạng thái";
        public static string OnboardingStep2Description => "Click vào các nút trạng thái để đánh dấu kết quả cuộc gọi.";

        public static string OnboardingStep3Title => "Xuất kết quả";
        public static string OnboardingStep3Description => "Nhấn Export Excel để lưu kết quả cuộc gọi ra file.";

        public static string Skip => "Bỏ qua";
        public static string Next => "Tiếp tục";
        public static string DontShowAgain => "Không hiển thị lại";
    }
}
