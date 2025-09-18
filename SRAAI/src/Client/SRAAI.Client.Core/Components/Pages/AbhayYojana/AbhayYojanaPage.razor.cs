using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using SRAAI.Shared.Dtos.AbhayYojana;
using SRAAI.Shared.Dtos.Summary;

namespace SRAAI.Client.Core.Components.Pages.AbhayYojana;

public partial class AbhayYojanaPage : AppPageBase
{
    private BitFileUpload fileUploadRef = default!;

    private bool isBusy = false;
    private bool isLoadingApplications = false;
    private bool isLoadingStatistics = false;
    
    private List<AbhayYojanaApplicationDto>? applications;
    private AbhayYojanaImportResult? lastImportResult;
    private AbhayYojanaStatistics? statistics;

    private List<SummaryDto>? summarydto;
    [Inject] private IJSRuntime JS { get; set; }
    [AutoInject] private HttpClient HttpClient = default!;
    [AutoInject] private DataService DataService = default!;
    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        await LoadApplications();
        await LoadStatistics();
    }

    private async Task LoadApplications()
    {
        try
        {
            isLoadingApplications = true;
            StateHasChanged();

            var url = new Uri(AbsoluteServerAddress, "/api/AbhayYojana/GetAll?page=1&pageSize=1000").ToString();
            var result = await HttpClient.GetFromJsonAsync<AbhayYojanaPagedResult>(url, JsonSerializerOptions, CurrentCancellationToken);
            applications = result?.Data ?? new List<AbhayYojanaApplicationDto>();
        }
        catch (Exception)
        {
            // Handle error
            applications = new List<AbhayYojanaApplicationDto>();
        }
        finally
        {
            isLoadingApplications = false;
            StateHasChanged();
        }
    }


    private async Task ExportToExcel()
    {
        try
        {
            var url = new Uri(AbsoluteServerAddress, "/api/AbhayYojana/Summary").ToString();
            summarydto = await HttpClient.GetFromJsonAsync<List<SummaryDto>>(
                url, JsonSerializerOptions, CurrentCancellationToken
            ) ?? new List<SummaryDto>();
        }
        catch (Exception)
        {
            summarydto = new List<SummaryDto>();
        }
        finally
        {
            isLoadingStatistics = false;
            StateHasChanged();
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Applications");

        ws.Cell(1, 1).Value =
            "शासन, गृहनिर्माण विभाग यांचेकडील निर्णय क्र. विसआ-2023/प्र.क्र. 159/झोपनि-2, दि. 01/10/2024 अन्वये दि. 01/01/2011 पूर्वी निर्गमित परिशिष्ट-II मधील हस्तांतरीत झोपडीधारकांचे हस्तांतरण नियमानुकूल करण्यासाठी एकवेळची अभय योजनेनुसार झोपडीधारकाचे पात्र/ अपात्रतेबाबत पुरवणी परिशिष्ट - II संस्थेचे नाव : मुंबई शहर जिल्हयातील माहिम महसूल विभागातील सि.टी.एस.क्र.  1500(पै) व इतर या मिळकतीवर असलेल्या नवकिरण वेल्फेअर एस. आर.ए.सहकारी गृहनिर्माण संस्था(नियोजित)(सदरचे पुरवणी परिशिष्ट - II या कार्यालयाचे पत्र क्र.सप्रा - 1 / टे - 1 - प्र / कावि - 1406 / 25, दि. 06 / 05 / 2025 सोबत वाचावे.)";

        ws.Range(1, 1, 1, 13).Merge().Style
            .Font.SetBold(true)
            .Font.SetFontSize(12)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Alignment.SetWrapText(true);

        ws.Cell(2, 1).Value = "अ.क्र.";
        ws.Cell(2, 2).Value = "मूळपरि - II मधील झो.क्र.";
        ws.Cell(2, 3).Value = "मूळ परिशिष्ट -II मधील झोपडीधारकाचे नाव";
        ws.Cell(2, 4).Value = "विहीत जोडपत्र दाखल केलेल्या अर्जदाराचे नाव";
        ws.Cell(2, 5).Value = "मतदार यादीमधील तपशील";
        ws.Range(2, 5, 2, 8).Merge();

        ws.Cell(2, 9).Value = "झोपडीचा वापर";
        ws.Cell(2, 10).Value = "झोपडी खालील चटई क्षेत्र (चौ.फु.)";
        ws.Cell(2, 11).Value = "अर्जदारांनी अभय योजनेनुसार सादर केलेल्या प्रमुख पुराव्यांचा तपशील";
        ws.Cell(2, 12).Value = "सुधारीत विकास नियंत्रण नियमावली 33(10) नुसार सक्षम प्राधिकरणाचे पात्रतेबाबत अभिप्राय";
        ws.Range(2, 12, 2, 13).Merge();

        ws.Cell(3, 5).Value = "वर्ष";
        ws.Cell(3, 6).Value = "भाग क्र.";
        ws.Cell(3, 7).Value = "मतदार यादीतील अनु. क्र.";
        ws.Cell(3, 8).Value = "मतदार यादीतील बांधकाम क्रमांक";
        ws.Cell(3, 12).Value = "पात्र/अपात्र/अनिश्चित";
        ws.Cell(3, 13).Value = "शेरा";

        string[] colNums = { "1", "2", "3", "4अ", "4ब", "4क", "4ड", "5", "6", "7", "8", "9", "10" };
        for (int i = 0; i < colNums.Length; i++)
            ws.Cell(4, i + 1).Value = colNums[i];

        var headerRange = ws.Range(2, 1, 4, 13);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        ws.SheetView.FreezeRows(4);

        int row = 5;
        foreach (var app in applications)
        {

            ws.Cell(row, 1).Value = app.SerialNumber;
            ws.Cell(row, 2).Value = app.OriginalSlumNumber;
            ws.Cell(row, 3).Value = app.OriginalSlumDwellerName;
            ws.Cell(row, 4).Value = app.ApplicantName;
            ws.Cell(row, 5).Value = app.VoterListYear;
            ws.Cell(row, 6).Value = app.VoterListPartNumber;
            ws.Cell(row, 7).Value = app.VoterListSerialNumber;
            ws.Cell(row, 8).Value = app.VoterListBound;
            ws.Cell(row, 9).Value = app.SlumUsage;
            ws.Cell(row, 10).Value = app.CarpetAreaSqFt;
            ws.Cell(row, 11).Value = app.EvidenceDetails;
            ws.Cell(row, 12).Value = app.EligibilityStatus;
            ws.Cell(row, 13).Value = app.Remarks;

            ws.Range(row, 1, row, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 1, row, 13).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(row, 1, row, 13).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, 13).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, 13).Style.Alignment.WrapText = true;

          /*  // Alternating row color
            if ((row - 4) % 2 == 0)
                ws.Range(row, 1, row, 13).Style.Fill.BackgroundColor = XLColor.LightGray;*/

            row++;

            ws.Range(row, 1, row, 13).Merge().Value =
                 "(अभिजीत भांडे-पाटील) सक्षम प्राधिकारी - 1, झोपडपट्टी पुनर्वसन प्राधिकरण, मुंबई";
            ws.Range(row, 1, row, 13).Style.Font.Bold = true;
            ws.Range(row, 1, row, 13).Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
            ws.Range(row, 1, row, 13).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            row++;
            ws.Cell(row, 1).Value = "सूचना:";
            ws.Range(row, 2, row, 13).Merge().Value =
                "1) प्रस्तुत झोपडपट्टीधारकाच्या पात्रतेबाबत व अन्य बाबी संबंधीचा निर्णय हा त्यांनी स्वयंसाक्षांकित पुरावे ग्राह्य धरुन तसेच सादर केलेल्या प्रतिज्ञापत्राआधारे दिलेला आहे. त्याचे कुटुंबातील सदस्यांचे बृहन्मुंबई म.न.पा. अंतर्गत घर असल्याचे अथवा यापूर्वी झोपडपट्टी पुनर्वसन योजना अगर अन्य शासकीय/निमशासकीय योजनेखाली लाभ मिळाला असल्यास अगर मिळण्याचे प्रस्तावित असल्यास त्यांनी केलेले पुरावे व प्रतिज्ञापत्र बनावट अगर खोटे असल्याचे निष्पन्न झाल्यास हा निर्णय आपोआप रद्द ठरेल व त्यांना मिळालेला लाभ झोपडपट्टी पुनर्वसन प्राधिकरण काढून घेईल व त्याचप्रमाणे त्यांचेविरद्ध भारतीय दंड विधानातील व अन्य प्रचलीत कायद्याआधारे फौजदारी गुन्हा दाखल होईल." +
                "2) सदर निर्णय झोपडीधारकास अमान्य असल्यास अगर सदर झोपडीधारकाच्या पात्रतेच्या निर्णयावर कोणाही भारतीय नागरीकाचा आक्षेप असल्यास, त्याबाबत महाराष्ट्र झोपडपट्टी क्षेत्र(सु.नि.व पु.) अधिनियम 1971 चे कलम 35 अन्वये अपर जिल्हाधिकारी तथा अपिलीय प्राधिकारी, मुंबई शहर यांचे प्राधिकरण, पहिला मजला, जुने जकात घर, शहिद भगतसिंग मार्ग, फोर्ट, मुंबई 400 001 यांचेकडे अपिल दाखल करण्याची तरतूद आहे.";
            ws.Range(row, 1, row, 13).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, 13).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 2, row, 13).Style.Alignment.WrapText = true;
            ws.Range(row, 2, row, 13).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            row++;           
        }

        ws.Columns().AdjustToContents();
        foreach (var col in ws.ColumnsUsed())
        {
            if (col.Width > 60) col.Width = 60; // max width
            col.Style.Alignment.WrapText = true;
        }
        ws.Rows().AdjustToContents();

        var usedRange = ws.RangeUsed();
        if (usedRange != null) usedRange.CreateTable();

        ws.SheetView.ZoomScale = 90;
        ws.PageSetup.PagesWide = 1;
        ws.PageSetup.PagesTall = 0;

        var summarySheet = workbook.Worksheets.Add("Summary");
        string[] headers = { "वापर", "निवासी", "अनिवासी", "संयुक्त", "धारस्थळ", "एकूण" };

        for (int i = 0; i < headers.Length; i++)
            summarySheet.Cell(1, i + 1).Value = headers[i];

        var summaryHeaderRange = summarySheet.Range(1, 1, 1, headers.Length);
        summaryHeaderRange.Style.Font.Bold = true;
        summaryHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        summaryHeaderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        int summaryRow = 2;
        foreach (var item in summarydto)
        {
            summarySheet.Cell(summaryRow, 1).Value = item.Category;
            summarySheet.Cell(summaryRow, 2).Value = item.Nivasi;
            summarySheet.Cell(summaryRow, 3).Value = item.Anivasi;
            summarySheet.Cell(summaryRow, 4).Value = item.Samyukt;
            summarySheet.Cell(summaryRow, 5).Value = item.Dharsthal;
            summarySheet.Cell(summaryRow, 6).Value = item.Total;
            summaryRow++;
        }

        summarySheet.Columns().AdjustToContents();
        var sumRange = summarySheet.RangeUsed();
        if (sumRange != null) sumRange.CreateTable();
        summarySheet.SheetView.ZoomScale = 90;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        await JS.InvokeVoidAsync("downloadFile",
            "Applications.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Convert.ToBase64String(bytes));
    }




    private async void GoToSummary()
    {
        try
        {
            NavigationManager.NavigateTo($"{PageUrls.SummaryPage}");
        }
        catch
        {}
    }
    private async Task LoadStatistics()
    {
        try
        {
            isLoadingStatistics = true;
            StateHasChanged();

            var url = new Uri(AbsoluteServerAddress, "/api/AbhayYojana/GetStatistics").ToString();
            statistics = await HttpClient.GetFromJsonAsync<AbhayYojanaStatistics>(url, JsonSerializerOptions, CurrentCancellationToken);
        }
        catch (Exception)
        {}
        finally
        {
            isLoadingStatistics = false;
            StateHasChanged();
        }
    }

    private async Task<string> GetUploadUrl()
    {
        return new Uri(AbsoluteServerAddress, "/api/AbhayYojana/ImportExcel").ToString();
    }

    private async Task<Dictionary<string, string>> GetUploadRequestHeaders()
    {
        var token = await AuthManager.GetFreshAccessToken(requestedBy: nameof(BitFileUpload));
        return new() { { "Authorization", $"Bearer {token}" } };
    }
    private Task HandleUploadFailed(BitFileInfo info)
    {
        isBusy = false;
        SnackBarService.Error(string.IsNullOrWhiteSpace(info.Message) ? "File upload failed" : info.Message);
        return Task.CompletedTask;
    }

    private async Task HandleUploadComplete(BitFileInfo info)
    {
        isBusy = false;
        try
        { 
            await LoadApplications();
            await LoadStatistics();
            
            // For now, just show success message
            // The import result will be displayed when the data is refreshed
            SnackBarService.Success("Excel imported successfully. Please check the results below.");
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            SnackBarService.Error($"Failed to process import result: {ex.Message}");
        }
    }

  
}

public record AbhayYojanaImportResult(
    int TotalRows,
    int SuccessfullyImported,
    int SkippedRows,
    List<string> Errors,
    List<string> Warnings
);

/*public record AbhayYojanaPagedResult(
    List<AbhayYojanaApplicationDto> Data,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);*/

public record AbhayYojanaStatistics(
    int TotalApplications,
    List<AbhayYojanaStatusBreakdown> StatusBreakdown,
    DateTime? LastUpdated
);

public record AbhayYojanaStatusBreakdown(
    string Status,
    int Count
);
