using System.Data;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SRAAI.Server.Api.Services.AbhayYojana;
using SRAAI.Shared.Dtos.AbhayYojana;
using SRAAI.Shared.Dtos.Summary;
using Syncfusion.XlsIO;

namespace SRAAI.Server.Api.Controllers.AbhayYojana;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AbhayYojanaController : AppControllerBase
{
    [AutoInject] private IAbhayYojanaExcelImportService excelImportService = default!;

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ImportExcel(IFormFile file, CancellationToken cancellationToken = default)
     {
        try
        {
            if (file is null || file.Length == 0)
                throw new BadRequestException("No file provided");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only Excel (.xlsx) files are supported");

            if (file.Length > 50 * 1024 * 1024)
                throw new BadRequestException("File size exceeds 50MB limit");

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;

                using (var inputStream = file.OpenReadStream())
                {
                    IWorkbook workbook = application.Workbooks.Open(inputStream);

                    // Get first worksheet
                    IWorksheet sheet1 = workbook.Worksheets[0];
                    // Get second worksheet (assuming it exists)
                    //IWorksheet sheet2 = workbook.Worksheets.Count > 1 ? workbook.Worksheets[1] : null;

                    /* if (sheet2 == null)
                         throw new BadRequestException("Excel file must have at least 2 sheets.");
 */
                    int headerRow = 4;
                    int firstCol = sheet1.UsedRange.Column;
                    int lastCol = 13;
                    int lastRow = sheet1.UsedRange.LastRow;

                    DataTable customersTable = sheet1.ExportDataTable(
                        headerRow,
                        firstCol,
                        lastRow,
                        lastCol,
                        ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);

                    /*   DataTable builderTable = sheet2.ExportDataTable(
                           headerRow,
                           firstCol,
                           lastRow,
                           lastCol,
                           ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);*/
                    var alldata = await DbContext.AbhayYojanaApplications.ToListAsync();
                    DbContext.AbhayYojanaApplications.RemoveRange(alldata);
                    await DbContext.SaveChangesAsync();
                    for (int i = 0; i < customersTable.Rows.Count; i += 3)
                    {
                        DataRow row = customersTable.Rows[i];
                        if (string.IsNullOrWhiteSpace(row[0]?.ToString()))
                            break;

                        int originalSlumNumber = Convert.ToInt32(row[1]?.ToString());

                        var presentdata = await DbContext.AbhayYojanaApplications
                            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber);

                        if (presentdata == null)
                        {
                         
                            var dto = new AbhayYojanaApplication
                            {
                                OriginalSlumNumber = originalSlumNumber,
                                SerialNumber = Convert.ToInt32(row[0]?.ToString()),
                                OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty,
                                ApplicantName = row[3]?.ToString() ?? string.Empty,
                                VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null,   
                                VoterListPartNumber = row[5]?.ToString(),
                                VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null, 
                                VoterListBound = row[7]?.ToString(),
                                SlumUsage = row[8]?.ToString() ?? string.Empty,
                                CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null,
                                EvidenceDetails = row[10]?.ToString() ?? string.Empty,
                                EligibilityStatus = row[11]?.ToString() ?? string.Empty,
                                Remarks = row[12]?.ToString(),
                                CreatedDate = DateTime.Now,
                            };

                            await DbContext.AbhayYojanaApplications.AddAsync(dto);
                        }
                        else
                        {
                           
                            presentdata.SerialNumber = Convert.ToInt32(row[0]?.ToString());
                            presentdata.OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty;
                            presentdata.ApplicantName = row[3]?.ToString() ?? string.Empty;
                            presentdata.VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null;
                            presentdata.VoterListPartNumber = row[5]?.ToString();
                            presentdata.VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null;
                            presentdata.VoterListBound = row[7]?.ToString();
                            presentdata.SlumUsage = row[8]?.ToString() ?? string.Empty;
                            presentdata.CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null;
                            presentdata.EvidenceDetails = row[10]?.ToString() ?? string.Empty;
                            presentdata.EligibilityStatus = row[11]?.ToString() ?? string.Empty;
                            presentdata.Remarks = row[12]?.ToString();
                            presentdata.UpdatedDate = DateTime.Now;

                      
                        }
                        await DbContext.SaveChangesAsync();
                    }


                    return Ok(new { message = "Excel imported successfully" });
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Import failed",
                message = ex.Message,
                details = ex.StackTrace
            });
        }
    }


    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ImportUpdateExcel(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file is null || file.Length == 0)
                throw new BadRequestException("No file provided");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only Excel (.xlsx) files are supported");

            if (file.Length > 50 * 1024 * 1024)
                throw new BadRequestException("File size exceeds 50MB limit");

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;

                using (var inputStream = file.OpenReadStream())
                {
                    IWorkbook workbook = application.Workbooks.Open(inputStream);

                    IWorksheet sheet1 = workbook.Worksheets[0];
                    int headerRow = 4;
                    int firstCol = sheet1.UsedRange.Column;
                    int lastCol = 13;
                    int lastRow = sheet1.UsedRange.LastRow;
                    DataTable customersTable = sheet1.ExportDataTable(
                        headerRow,
                        firstCol,
                        lastRow,
                        lastCol,
                        ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);
                        

                    for (int i = 0; i < customersTable.Rows.Count; i += 3)
                    {
                        DataRow row = customersTable.Rows[i];
                        if (string.IsNullOrWhiteSpace(row[0]?.ToString()))
                            break;

                        int originalSlumNumber = Convert.ToInt32(row[1]?.ToString());

                        var presentdata = await DbContext.AbhayYojanaApplications
                            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber);

                        if (presentdata != null)
                        {
                            presentdata.SerialNumber = Convert.ToInt32(row[0]?.ToString());
                            presentdata.OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty;
                            presentdata.ApplicantName = row[3]?.ToString() ?? string.Empty;
                            presentdata.VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null;
                            presentdata.VoterListPartNumber = row[5]?.ToString();
                            presentdata.VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null;
                            presentdata.VoterListBound = row[7]?.ToString();
                            presentdata.SlumUsage = row[8]?.ToString() ?? string.Empty;
                            presentdata.CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null;
                            presentdata.EvidenceDetails = row[10]?.ToString() ?? string.Empty;
                            presentdata.EligibilityStatus = row[11]?.ToString() ?? string.Empty;
                            presentdata.Remarks = row[12]?.ToString();
                            presentdata.UpdatedDate = DateTime.Now;


                        }
                        DbContext.Update(presentdata);
                        await DbContext.SaveChangesAsync();
                    }


                    return Ok(new { message = "Excel imported successfully" });
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Import failed",
                message = ex.Message,
                details = ex.StackTrace
            });
        }
    }


    [HttpGet()]
    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken = default)
    {
        var applications = await DbContext.AbhayYojanaApplications.ToListAsync();
        var summarydto = await Summary(cancellationToken);
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

             /* // Alternating row color
              if ((row - 4) % 2 == 0)
                ws.Range(row, 1, row, 13).Style.Fill.BackgroundColor = XLColor.LightGray; *//**/

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
        stream.Seek(0, SeekOrigin.Begin);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Applications.xlsx");
    }


    public async Task<List<AbhayYojanaApplicationDto>> BuilderExcelScanning(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("No file provided");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Only Excel (.xlsx) files are supported");

        if (file.Length > 50 * 1024 * 1024)
            throw new BadRequestException("File size exceeds 50MB limit");

        using (ExcelEngine excelEngine = new ExcelEngine())
        {
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            using var inputStream = file.OpenReadStream();
            IWorkbook workbook = application.Workbooks.Open(inputStream);
            IWorksheet sheet1 = workbook.Worksheets[0];

            int headerRow = 4;
            int firstCol = sheet1.UsedRange.Column;
            int lastCol = 13;
            int lastRow = sheet1.UsedRange.LastRow;

            DataTable customersTable = sheet1.ExportDataTable(
                headerRow,
                firstCol,
                lastRow,
                lastCol,
                ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);

            var dbData = await DbContext.AbhayYojanaApplications.ToListAsync(cancellationToken);

            List<AbhayYojanaApplicationDto> result = new();

         
            for (int i = 0; i < customersTable.Rows.Count; i += 3)
            {
                DataRow row = customersTable.Rows[i];

                if (string.IsNullOrWhiteSpace(row[0]?.ToString())) break;

                int originalSlumNumber = Convert.ToInt32(row[1]?.ToString());

                var dto = new AbhayYojanaApplicationDto
                {
                    OriginalSlumNumber = originalSlumNumber,
                    SerialNumber = Convert.ToInt32(row[0]?.ToString()),
                    OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty,
                    ApplicantName = row[3]?.ToString() ?? string.Empty,
                    VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null,
                    VoterListPartNumber = row[5]?.ToString(),
                    VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null,
                    VoterListBound = row[7]?.ToString(),
                    SlumUsage = row[8]?.ToString() ?? string.Empty,
                    CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null,
                    EvidenceDetails = row[10]?.ToString() ?? string.Empty,
                    EligibilityStatus = row[11]?.ToString() ?? string.Empty,
                    Remarks = row[12]?.ToString(),
                };

                var originaldata = dbData.FirstOrDefault(a => a.OriginalSlumNumber == originalSlumNumber);

                if (originaldata != null)
                {
                    var changed = new List<string>();

                    if (originaldata.SerialNumber != dto.SerialNumber) changed.Add(nameof(dto.SerialNumber));
                    if (originaldata.OriginalSlumDwellerName != dto.OriginalSlumDwellerName) changed.Add(nameof(dto.OriginalSlumDwellerName));
                    if (originaldata.ApplicantName != dto.ApplicantName) changed.Add(nameof(dto.ApplicantName));
                    if (originaldata.SlumUsage != dto.SlumUsage) changed.Add(nameof(dto.SlumUsage));
                    if (originaldata.EvidenceDetails != dto.EvidenceDetails) changed.Add(nameof(dto.EvidenceDetails));
                    if (originaldata.EligibilityStatus != dto.EligibilityStatus) changed.Add(nameof(dto.EligibilityStatus));
                    if (originaldata.Remarks != dto.Remarks) changed.Add(nameof(dto.Remarks));

                    if (changed.Any())
                    {
                        dto.Status = "NotMatching";
                        dto.ChangedFields = changed;
                    }
                    else
                    {
                        dto.Status = "Matching";
                    }
                }
                else
                {
                    dto.Status = "Additional"; 
                }

                result.Add(dto);
            }

            
            var excelSlumNumbers = customersTable.Rows
     .Cast<DataRow>()
     .Select(r => r[1]?.ToString())
     .Where(s => !string.IsNullOrWhiteSpace(s) && int.TryParse(s, out _))
     .Select(s => int.Parse(s!))
     .ToHashSet();


            var notFound = dbData
                .Where(db => !excelSlumNumbers.Contains(db.OriginalSlumNumber))
                .Select(db => new AbhayYojanaApplicationDto
                {
                    OriginalSlumNumber = db.OriginalSlumNumber,
                    SerialNumber = db.SerialNumber,
                    OriginalSlumDwellerName = db.OriginalSlumDwellerName,
                    ApplicantName = db.ApplicantName,
                    VoterListYear = db.VoterListYear,
                    VoterListPartNumber = db.VoterListPartNumber,
                    VoterListSerialNumber = db.VoterListSerialNumber,
                    SlumUsage = db.SlumUsage,
                    CarpetAreaSqFt = db.CarpetAreaSqFt,
                    EvidenceDetails = db.EvidenceDetails,
                    EligibilityStatus = db.EligibilityStatus,
                    Remarks = db.Remarks,
                    Status = "NotFound"
                });

            result.AddRange(notFound);

            return result;
        }
    }



    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = DbContext.AbhayYojanaApplications.AsQueryable();
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var applications = await query
            .OrderBy(a => a.OriginalSlumNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AbhayYojanaApplicationDto
            {
                SerialNumber = a.SerialNumber,
                OriginalSlumNumber = a.OriginalSlumNumber,
                OriginalSlumDwellerName = a.OriginalSlumDwellerName,
                ApplicantName = a.ApplicantName,
                VoterListYear = a.VoterListYear,
                VoterListPartNumber = a.VoterListPartNumber,
                VoterListSerialNumber = a.VoterListSerialNumber,
                VoterListBound = a.VoterListBound,
                SlumUsage = a.SlumUsage,
                CarpetAreaSqFt = a.CarpetAreaSqFt,
                EvidenceDetails = a.EvidenceDetails,
                EligibilityStatus = a.EligibilityStatus,
                Remarks = a.Remarks,
                CreatedDate = a.CreatedDate,
                UpdatedDate = a.UpdatedDate,
                Version = a.Version
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Data = applications,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

 

  /*  [HttpGet]
    public async Task<List<AbhayYojanaApplicationDto>> GetAbhayYojanaApplication(CancellationToken cancellationToken)
    {
        var data = await DbContext.AbhayYojanaApplications.ToListAsync(cancellationToken);

        return
    }*/


    [HttpGet("{originalSlumNumber}")]
    public async Task<IActionResult> GetBySlumNumber(int originalSlumNumber, CancellationToken cancellationToken = default)
    {
        var application = await DbContext.AbhayYojanaApplications
            .Where(a => a.OriginalSlumNumber == originalSlumNumber)
            .Select(a => new AbhayYojanaApplicationDto
            {
                SerialNumber = a.SerialNumber,
                OriginalSlumNumber = a.OriginalSlumNumber,
                OriginalSlumDwellerName = a.OriginalSlumDwellerName,
                ApplicantName = a.ApplicantName,
                VoterListYear = a.VoterListYear,
                VoterListPartNumber = a.VoterListPartNumber,
                VoterListSerialNumber = a.VoterListSerialNumber,
                VoterListBound = a.VoterListBound,
                SlumUsage = a.SlumUsage,
                CarpetAreaSqFt = a.CarpetAreaSqFt,
                EvidenceDetails = a.EvidenceDetails,
                EligibilityStatus = a.EligibilityStatus,
                Remarks = a.Remarks,
                CreatedDate = a.CreatedDate,
                UpdatedDate = a.UpdatedDate,
                Version = a.Version
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (application == null)
            return NotFound($"Application with slum number {originalSlumNumber} not found");

        return Ok(application);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAbhayYojanaApplicationDto dto, CancellationToken cancellationToken = default)
    {
        // Check if slum number already exists
        var exists = await DbContext.AbhayYojanaApplications
            .AnyAsync(a => a.OriginalSlumNumber == dto.OriginalSlumNumber, cancellationToken);

        if (exists)
            throw new BadRequestException($"Application with slum number {dto.OriginalSlumNumber} already exists");

        var application = new Data.AbhayYojanaApplication
        {
            SerialNumber = dto.SerialNumber,
            OriginalSlumNumber = dto.OriginalSlumNumber,
            OriginalSlumDwellerName = dto.OriginalSlumDwellerName,
            ApplicantName = dto.ApplicantName,
            VoterListYear = dto.VoterListYear,
            VoterListPartNumber = dto.VoterListPartNumber,
            VoterListSerialNumber = dto.VoterListSerialNumber,
            VoterListBound = dto.VoterListBound,
            SlumUsage = dto.SlumUsage,
            CarpetAreaSqFt = dto.CarpetAreaSqFt,
            EvidenceDetails = dto.EvidenceDetails,
            EligibilityStatus = dto.EligibilityStatus,
            Remarks = dto.Remarks,
            Version = dto.Version,
            CreatedDate = DateTime.UtcNow
        };

        await DbContext.AbhayYojanaApplications.AddAsync(application, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBySlumNumber), new { originalSlumNumber = application.OriginalSlumNumber }, application);
    }

    [HttpPut("{originalSlumNumber}")]
    public async Task<IActionResult> Update(int originalSlumNumber, [FromBody] UpdateAbhayYojanaApplicationDto dto, CancellationToken cancellationToken = default)
    {
        var application = await DbContext.AbhayYojanaApplications
            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber, cancellationToken);

        if (application == null)
            return NotFound($"Application with slum number {originalSlumNumber} not found");

        // Update only provided fields
        if (dto.OriginalSlumDwellerName != null)
            application.OriginalSlumDwellerName = dto.OriginalSlumDwellerName;
        
        if (dto.ApplicantName != null)
            application.ApplicantName = dto.ApplicantName;
        
        if (dto.VoterListYear.HasValue)
            application.VoterListYear = dto.VoterListYear;
        
        if (dto.VoterListPartNumber != null)
            application.VoterListPartNumber = dto.VoterListPartNumber;
        
        if (dto.VoterListSerialNumber.HasValue)
            application.VoterListSerialNumber = dto.VoterListSerialNumber;
        
        if (dto.VoterListBound != null)
            application.VoterListBound = dto.VoterListBound;
        
        if (dto.SlumUsage != null)
            application.SlumUsage = dto.SlumUsage;
        
        if (dto.CarpetAreaSqFt.HasValue)
            application.CarpetAreaSqFt = dto.CarpetAreaSqFt;
        
        if (dto.EvidenceDetails != null)
            application.EvidenceDetails = dto.EvidenceDetails;
        
        if (dto.EligibilityStatus != null)
            application.EligibilityStatus = dto.EligibilityStatus;
        
        if (dto.Remarks != null)
            application.Remarks = dto.Remarks;

        application.UpdatedDate = DateTime.UtcNow;

        await DbContext.SaveChangesAsync(cancellationToken);

        return Ok(application);
    }

    [HttpDelete("{originalSlumNumber}")]
    public async Task<IActionResult> Delete(int originalSlumNumber, CancellationToken cancellationToken = default)
    {
        var application = await DbContext.AbhayYojanaApplications
            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber, cancellationToken);

        if (application == null)
            return NotFound($"Application with slum number {originalSlumNumber} not found");

        DbContext.AbhayYojanaApplications.Remove(application);
        await DbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
    {
        var stats = await DbContext.AbhayYojanaApplications
            .GroupBy(a => a.EligibilityStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var totalCount = await DbContext.AbhayYojanaApplications.CountAsync(cancellationToken);

        return Ok(new
        {
            TotalApplications = totalCount,
            StatusBreakdown = stats,
            LastUpdated = await DbContext.AbhayYojanaApplications
                .MaxAsync(a => (DateTime?)a.UpdatedDate ?? a.CreatedDate, cancellationToken)
        });
    }

    [HttpGet]
    public async Task<List<SummaryDto>> Summary(CancellationToken cancellationToken)
    {
        var data = await DbContext.AbhayYojanaApplications.ToListAsync(cancellationToken);

        // Fixed categories you want to show
        var categories = new[] { "पात्र", "अपात्र", "अनिर्णित" };
        var result = new List<SummaryDto>();

        foreach (var cat in categories)
        {
            var rows = data.Where(x =>
            {
                if (string.IsNullOrWhiteSpace(x.EligibilityStatus))
                    return false;

                var status = x.EligibilityStatus.Trim();

                // Normalize DB values
                if (status == "पात्र नाही")
                    status = "अपात्र";

                return status.Equals(cat, StringComparison.OrdinalIgnoreCase);
            });

            result.Add(new SummaryDto
            {
                Category = cat,
                Nivasi = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                         x.SlumUsage.Trim().Equals("निवासी", StringComparison.OrdinalIgnoreCase)),
                Anivasi = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                          x.SlumUsage.Trim().Equals("अनिवासी", StringComparison.OrdinalIgnoreCase)),
                Samyukt = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                          x.SlumUsage.Trim().Equals("संयुक्त", StringComparison.OrdinalIgnoreCase)),
                Dharsthal = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                            x.SlumUsage.Trim().Equals("धारस्थळ", StringComparison.OrdinalIgnoreCase))
            });
        }

        // Add total row
        result.Add(new SummaryDto
        {
            Category = "एकूण",
            Nivasi = result.Sum(r => r.Nivasi),
            Anivasi = result.Sum(r => r.Anivasi),
            Samyukt = result.Sum(r => r.Samyukt),
            Dharsthal = result.Sum(r => r.Dharsthal)
        });

        return result;
    }


}
