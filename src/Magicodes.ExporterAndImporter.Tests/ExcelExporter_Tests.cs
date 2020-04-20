// ======================================================================
// 
//           filename : ExcelExporter_Tests.cs
//           description :
// 
//           created by 雪雁 at  2019-09-11 13:51
//           文档官网：https://docs.xin-lai.com
//           公众号教程：麦扣聊技术
//           QQ群：85318032（编程交流）
//           Blog：http://www.cnblogs.com/codelove/
// 
// ======================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using Magicodes.ExporterAndImporter.Tests.Models.Export;
using OfficeOpenXml;
using Shouldly;
using Xunit;
using Magicodes.ExporterAndImporter.Core.Extension;
using Magicodes.ExporterAndImporter.Core.Models;
using Magicodes.ExporterAndImporter.Csv;
using Magicodes.ExporterAndImporter.Tests.Models.Export.ExportByTemplate_Test1;
using OfficeOpenXml.Drawing;

namespace Magicodes.ExporterAndImporter.Tests
{
    public class ExcelExporter_Tests : TestBase
    {
        /// <summary>
        ///     将entities直接转成DataTable
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entities">entity集合</param>
        /// <returns>将Entity的值转为DataTable</returns>
        private static DataTable EntityToDataTable<T>(DataTable dt, IEnumerable<T> entities)
        {
            if (!entities.Any()) return dt;

            var properties = typeof(T).GetProperties();

            foreach (var entity in entities)
            {
                var dr = dt.NewRow();

                foreach (var property in properties)
                    if (dt.Columns.Contains(property.Name))
                        dr[property.Name] = property.GetValue(entity, null);

                dt.Rows.Add(dr);
            }
            return dt;
        }

        [Fact(DisplayName = "DTO特性导出（测试格式化）")]
        public async Task AttrsExport_Test()
        {
            IExporter exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(AttrsExport_Test)}.xlsx");

            DeleteFile(filePath);

            var data = GenFu.GenFu.ListOf<ExportTestDataWithAttrs>(100);
            foreach (var item in data)
            {
                item.LongNo = 458752665;
            }
            var result = await exporter.Export(filePath, data);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                pck.Workbook.Worksheets.Count.ShouldBe(1);
                var sheet = pck.Workbook.Worksheets.First();
                sheet.Cells[sheet.Dimension.Address].Rows.ShouldBe(101);
                sheet.Cells["A2"].Text.ShouldBe(data[0].Text);

                //[ExporterHeader(DisplayName = "日期1", Format = "yyyy-MM-dd")]
                sheet.Cells["E2"].Text.Equals(DateTime.Parse(sheet.Cells["E2"].Text).ToString("yyyy-MM-dd"));

                //[ExporterHeader(DisplayName = "日期2", Format = "yyyy-MM-dd HH:mm:ss")]
                sheet.Cells["F2"].Text.Equals(DateTime.Parse(sheet.Cells["F2"].Text).ToString("yyyy-MM-dd HH:mm:ss"));

                //默认DateTime
                sheet.Cells["G2"].Text.Equals(DateTime.Parse(sheet.Cells["G2"].Text).ToString("yyyy-MM-dd"));

                sheet.Tables.Count.ShouldBe(1);

                var tb = sheet.Tables.First();
                tb.Columns.Count.ShouldBe(9);
                tb.Columns.First().Name.ShouldBe("加粗文本");
            }
        }

        [Fact(DisplayName = "空数据导出")]
        public async Task AttrsExportWithNoData_Test()
        {
            IExporter exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(AttrsExportWithNoData_Test)}.xlsx");

            DeleteFile(filePath);

            var data = new List<ExportTestDataWithAttrs>();
            var result = await exporter.Export(filePath, data);

            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                pck.Workbook.Worksheets.Count.ShouldBe(1);
                pck.Workbook.Worksheets.First().Cells[pck.Workbook.Worksheets.First().Dimension.Address].Rows.ShouldBe(1);
            }
        }

        [Fact(DisplayName = "数据拆分多Sheet导出")]
        public async Task SplitData_Test()
        {
            IExporter exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(SplitData_Test)}-1.xlsx");

            DeleteFile(filePath);

            var result = await exporter.Export(filePath,
                GenFu.GenFu.ListOf<ExportTestDataWithSplitSheet>(300));

            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //验证Sheet数是否为3
                pck.Workbook.Worksheets.Count.ShouldBe(3);
                //检查忽略列
                pck.Workbook.Worksheets.First().Cells["C1"].Value.ShouldBe("数值");
                pck.Workbook.Worksheets.First().Cells[pck.Workbook.Worksheets.First().Dimension.Address].Rows.ShouldBe(101);
            }

            filePath = GetTestFilePath($"{nameof(SplitData_Test)}-2.xlsx");
            DeleteFile(filePath);

            result = await exporter.Export(filePath,
                GenFu.GenFu.ListOf<ExportTestDataWithSplitSheet>(299));

            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //验证Sheet数是否为3
                pck.Workbook.Worksheets.Count.ShouldBe(3);
                //请不要使用索引（NET461和.NET Core的Sheet索引值不一致）
                var lastSheet = pck.Workbook.Worksheets.Last();
                lastSheet.Cells[lastSheet.Dimension.Address].Rows.ShouldBe(100);
            }

            filePath = GetTestFilePath($"{nameof(SplitData_Test)}-3.xlsx");
            DeleteFile(filePath);

            result = await exporter.Export(filePath,
                GenFu.GenFu.ListOf<ExportTestDataWithSplitSheet>(302));

            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //验证Sheet数是否为4
                pck.Workbook.Worksheets.Count.ShouldBe(4);
                //请不要使用索引（NET461和.NET Core的Sheet索引值不一致）
                var lastSheet = pck.Workbook.Worksheets.Last();
                lastSheet.Cells[lastSheet.Dimension.Address].Rows.ShouldBe(3);
            }
        }

        [Fact(DisplayName = "头部筛选器测试")]
        public async Task ExporterHeaderFilter_Test()
        {
            IExporter exporter = new ExcelExporter();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{nameof(ExporterHeaderFilter_Test)}.xlsx");
            #region 通过筛选器修改列名
            if (File.Exists(filePath)) File.Delete(filePath);

            var data1 = GenFu.GenFu.ListOf<ExporterHeaderFilterTestData1>();
            var result = await exporter.Export(filePath, data1);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();

            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                sheet.Cells["D1"].Value.ShouldBe("name");
                sheet.Dimension.Columns.ShouldBe(4);
            }
            #endregion

            #region 通过筛选器修改忽略列
            if (File.Exists(filePath)) File.Delete(filePath);
            var data2 = GenFu.GenFu.ListOf<ExporterHeaderFilterTestData2>();
            result = await exporter.Export(filePath, data2);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();

            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                sheet.Dimension.Columns.ShouldBe(5);
            }
            #endregion
        }

        [Fact(DisplayName = "DataTable结合DTO导出Excel")]
        public async Task DynamicExport_Test()
        {
            IExporter exporter = new ExcelExporter();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameof(DynamicExport_Test) + ".xlsx");
            if (File.Exists(filePath)) File.Delete(filePath);

            var exportDatas = GenFu.GenFu.ListOf<ExportTestDataWithAttrs>(1000);
            var dt = exportDatas.ToDataTable();
            var result = await exporter.Export<ExportTestDataWithAttrs>(filePath, dt);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                sheet.Dimension.Columns.ShouldBe(9);
            }
        }

        [Fact(DisplayName = "DataTable导出Excel（无需定义类，支持列筛选器和表拆分）")]
        public async Task DynamicDataTableExport_Test()
        {
            IExcelExporter exporter = new ExcelExporter();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameof(DynamicDataTableExport_Test) + ".xlsx");
            if (File.Exists(filePath)) File.Delete(filePath);

            var exportDatas = GenFu.GenFu.ListOf<ExportTestDataWithAttrs>(50);
            var dt = new DataTable();
            //创建带列名和类型名的列
            dt.Columns.Add("Text", Type.GetType("System.String"));
            dt.Columns.Add("Name", Type.GetType("System.String"));
            dt.Columns.Add("Number", Type.GetType("System.Decimal"));
            dt = EntityToDataTable(dt, exportDatas);
            //加个筛选器导出
            var result = await exporter.Export(filePath, dt, new DataTableTestExporterHeaderFilter(), 10);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //判断Sheet拆分
                pck.Workbook.Worksheets.Count.ShouldBe(5);
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                sheet.Cells["C1"].Value.ShouldBe("数值");
                sheet.Dimension.Columns.ShouldBe(3);
            }
        }

#if DEBUG
        [Fact(DisplayName = "大量数据导出Excel", Skip = "本地Debug模式下跳过，太费时")]
#else
        [Fact(DisplayName = "大量数据导出Excel")]
#endif
        public async Task Export100000Data_Test()
        {
            IExporter exporter = new ExcelExporter();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameof(Export100000Data_Test) + ".xlsx");
            if (File.Exists(filePath)) File.Delete(filePath);

            var result = await exporter.Export(filePath, GenFu.GenFu.ListOf<ExportTestData>(100000));
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
        }

        [Fact(DisplayName = "DTO导出")]
        public async Task ExportAsByteArray_Test()
        {
            IExporter exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(ExportAsByteArray_Test)}.xlsx");

            DeleteFile(filePath);

            var result = await exporter.ExportAsByteArray(GenFu.GenFu.ListOf<ExportTestDataWithAttrs>());
            result.ShouldNotBeNull();
            result.Length.ShouldBeGreaterThan(0);
            File.WriteAllBytes(filePath, result);
            File.Exists(filePath).ShouldBeTrue();
        }


        [Fact(DisplayName = "多个sheet导出")]
        public async Task ExportMutiCollection_Test()
        {
            var exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(ExportMutiCollection_Test)}.xlsx");

            DeleteFile(filePath);


            var list1 = GenFu.GenFu.ListOf<ExportTestDataWithAttrs>();

            var list2 = GenFu.GenFu.ListOf<ExportTestDataWithSplitSheet>(30);


            var result = exporter.Append(list1).Append(list2).ExportAppendData(filePath);
            result.ShouldNotBeNull();

            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                pck.Workbook.Worksheets.Count.ShouldBe(2);
                pck.Workbook.Worksheets.First().Name.ShouldBe(typeof(ExportTestDataWithAttrs).GetAttribute<ExcelExporterAttribute>().Name);
                pck.Workbook.Worksheets.Last().Name.ShouldBe(typeof(ExportTestDataWithSplitSheet).GetAttribute<ExcelExporterAttribute>().Name);
            }

        }

        [Fact(DisplayName = "多个sheet导出（空数据）")]
        public async Task ExportMutiCollectionWithEmpty_Test()
        {
            var exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(ExportMutiCollectionWithEmpty_Test)}.xlsx");

            DeleteFile(filePath);


            var list1 = new List<ExportTestDataWithAttrs>();

            var list2 = new List<ExportTestDataWithSplitSheet>();


            var result = exporter.Append(list1).Append(list2).ExportAppendData(filePath);
            result.ShouldNotBeNull();

            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                pck.Workbook.Worksheets.Count.ShouldBe(2);
            }

        }


        [Fact(DisplayName = "通过Dto导出表头")]
        public async Task ExportHeaderAsByteArray_Test()
        {
            IExporter exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(ExportHeaderAsByteArray_Test)}.xlsx");

            DeleteFile(filePath);

            var result = await exporter.ExportHeaderAsByteArray(GenFu.GenFu.New<ExportTestDataWithAttrs>());
            result.ShouldNotBeNull();
            result.Length.ShouldBeGreaterThan(0);
            result.ToExcelExportFileInfo(filePath);
            File.Exists(filePath).ShouldBeTrue();

            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                sheet.Name.ShouldBe("测试");
                sheet.Dimension.Columns.ShouldBe(9);
            }
        }

        [Fact(DisplayName = "通过动态传值导出表头")]
        public async Task ExportHeaderAsByteArrayWithItems_Test()
        {
            IExcelExporter exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(ExportHeaderAsByteArrayWithItems_Test)}.xlsx");

            DeleteFile(filePath);
            var arr = new[] { "Name1", "Name2", "Name3", "Name4", "Name5", "Name6" };
            var sheetName = "Test";
            var result = await exporter.ExportHeaderAsByteArray(arr, sheetName);
            result.ShouldNotBeNull();
            result.Length.ShouldBeGreaterThan(0);
            result.ToExcelExportFileInfo(filePath);
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                sheet.Name.ShouldBe(sheetName);
                sheet.Dimension.Columns.ShouldBe(arr.Length);
            }
        }


#if DEBUG
        [Fact(DisplayName = "大数据动态列导出Excel", Skip = "本地Debug模式下跳过，太费时")]
#else
        [Fact(DisplayName = "大数据动态列导出Excel")]
#endif
        public async Task LargeDataDynamicExport_Test()
        {
            IExcelExporter exporter = new ExcelExporter();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameof(LargeDataDynamicExport_Test) + ".xlsx");
            if (File.Exists(filePath)) File.Delete(filePath);

            var exportDatas = GenFu.GenFu.ListOf<ExportTestDataWithAttrs>(1200000);

            var dt = new DataTable();
            //创建带列名和类型名的列
            dt.Columns.Add("Text", Type.GetType("System.String"));
            dt.Columns.Add("Name", Type.GetType("System.String"));
            dt.Columns.Add("Number", Type.GetType("System.Decimal"));
            dt = EntityToDataTable(dt, exportDatas);

            var result = await exporter.Export(filePath, dt, maxRowNumberOnASheet: 100000);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //判断Sheet拆分
                pck.Workbook.Worksheets.Count.ShouldBe(12);
            }
        }

        #region 模板导出
        [Fact(DisplayName = "Excel模板导出教材订购明细样表（含图片）")]
        public async Task ExportByTemplate_Test()
        {
            //模板路径
            var tplPath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "ExportTemplates",
                "2020年春季教材订购明细样表.xlsx");
            //创建Excel导出对象
            IExportFileByTemplate exporter = new ExcelExporter();
            //导出路径
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameof(ExportByTemplate_Test) + ".xlsx");
            if (File.Exists(filePath)) File.Delete(filePath);
            //根据模板导出
            await exporter.ExportByTemplate(filePath,
                new TextbookOrderInfo("湖南心莱信息科技有限公司", "湖南长沙岳麓区", "雪雁", "1367197xxxx", null, DateTime.Now.ToLongDateString(), "https://docs.microsoft.com/en-us/media/microsoft-logo-dark.png",
                    new List<BookInfo>()
                    {
                        new BookInfo(1, "0000000001", "《XX从入门到放弃》", null, "机械工业出版社", "3.14", 100, "备注"){
                            Cover = Path.Combine("TestFiles", "ExporterTest.png")
                        },
                        new BookInfo(2, "0000000002", "《XX从入门到放弃》", "张三", "机械工业出版社", "3.14", 100, null),
                        new BookInfo(3, null, "《XX从入门到放弃》", "张三", "机械工业出版社", "3.14", 100, "备注")
                        {
                            Cover = Path.Combine("TestFiles", "ExporterTest.png")
                        }
                    }),
                tplPath);

            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                //确保所有的转换均已完成
                sheet.Cells[sheet.Dimension.Address].Any(p => p.Text.Contains("{{")).ShouldBeFalse();
                //检查图片
                sheet.Drawings.Count.ShouldBe(3);

            }
        }

        /// <summary>
        /// https://github.com/dotnetcore/Magicodes.IE/issues/34
        /// </summary>
        /// <returns></returns>
        [Fact(DisplayName = "Excel模板导出测试（issues#34）")]
        public async Task ExportByTemplate_Test1()
        {
            //模板路径
            var tplPath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "ExportTemplates",
                "template.xlsx");
            //创建Excel导出对象
            IExportFileByTemplate exporter = new ExcelExporter();
            //导出路径
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameof(ExportByTemplate_Test1) + ".xlsx");
            if (File.Exists(filePath)) File.Delete(filePath);

            var airCompressors = new List<AirCompressor>
            {
                new AirCompressor()
                {
                    Name = "1#",
                    Manufactor = "111",
                    ExhaustPressure = "0",
                    ExhaustTemperature = "66.7-95",
                    RunningTime = "35251",
                    WarningError = "正常",
                    Status = "开机"
                },
                new AirCompressor()
                {
                    Name = "2#",
                    Manufactor = "222",
                    ExhaustPressure = "1",
                    ExhaustTemperature = "90.7-95",
                    RunningTime = "2222",
                    WarningError = "正常",
                    Status = "开机"
                }
            };

            var afterProcessings = new List<AfterProcessing>
            {
                new AfterProcessing()
                {
                    Name = "1#abababa",
                    Manufactor = "杭州立山",
                    RunningTime = "NaN",
                    WarningError = "故障",
                    Status = "停机"
                }
            };

            var suggests = new List<Suggest>
            {
                new Suggest()
                {
                    Number = 1,
                    Description = "故障停机",
                    SuggestMessage = "顾问团队远程协助"
                }
            };

            //根据模板导出
            await exporter.ExportByTemplate(filePath,
                    new ReportInformation()
                    {
                        Contacts = "11112",
                        ContactsNumber = "13642666666",
                        CustomerName = "ababace",
                        Date = DateTime.Now.ToString("yyyy年MM月dd日"),
                        SystemExhaustPressure = "0.54-0.62",
                        SystemDewPressure = "-0.63--77.5",
                        SystemDayFlow = "201864",
                        AirCompressors = airCompressors,
                        AfterProcessings = afterProcessings,
                        Suggests = suggests,
                        SystemPressureHisotries = new List<SystemPressureHisotry>()
                    },
                    tplPath);

            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                //确保所有的转换均已完成
                sheet.Cells[sheet.Dimension.Address].Any(p => p.Text.Contains("{{")).ShouldBeFalse();
            }
        }
#if DEBUG
        [Fact(DisplayName = "Excel模板大量导出", Skip = "本地Debug模式下跳过，太费时")]
#else
        [Fact(DisplayName = "Excel模板大量导出")]
#endif
        public async Task ExportByTemplate_Large_Test()
        {
            //导出5000条数据不超过1秒
            var tplPath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "ExportTemplates",
                "2020年春季教材订购明细样表.xlsx");
            IExportFileByTemplate exporter = new ExcelExporter();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameof(ExportByTemplate_Large_Test) + ".xlsx");
            if (File.Exists(filePath)) File.Delete(filePath);

            var books = new List<BookInfo>();
            for (int i = 0; i < 5000; i++)
            {
                books.Add(new BookInfo(i + 1, "0000000" + i, "《XX从入门到放弃》", "张三", "机械工业出版社", "3.14", 100 + i, "备注"));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await exporter.ExportByTemplate(filePath, new TextbookOrderInfo("湖南心莱信息科技有限公司", "湖南长沙岳麓区", "雪雁", "1367197xxxx", "雪雁", DateTime.Now.ToLongDateString(), "https://docs.microsoft.com/en-us/media/microsoft-logo-dark.png", books), tplPath);
            stopwatch.Stop();
            //执行时间不得超过1秒（受实际执行机器性能影响）,在测试管理器中运行普遍小于400ms
            //stopwatch.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(1000);

        }

        #endregion

        [Fact(DisplayName = "无特性定义导出测试")]
        public async Task ExportTestDataWithoutExcelExporter_Test()
        {
            IExporter exporter = new ExcelExporter();
            var filePath = GetTestFilePath($"{nameof(ExportTestDataWithoutExcelExporter_Test)}.xlsx");
            DeleteFile(filePath);

            var result = await exporter.Export(filePath,
                GenFu.GenFu.ListOf<ExportTestDataWithoutExcelExporter>());
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
        }

        #region 图片导出
        [Fact(DisplayName = "Excel导出图片测试")]
        public async Task ExportPicture_Test()
        {
            IExporter exporter = new ExcelExporter();
            var filePath = GetTestFilePath($"{nameof(ExportPicture_Test)}.xlsx");
            DeleteFile(filePath);
            var data = GenFu.GenFu.ListOf<ExportTestDataWithPicture>(5);
            var url = Path.Combine("TestFiles", "ExporterTest.png");
            for (var i = 0; i < data.Count; i++)
            {
                var item = data[i];
                item.Img1 = url;
                if (i == 4)
                    item.Img = null;
                else
                    item.Img = "https://docs.microsoft.com/en-us/media/microsoft-logo-dark.png";
            }
            var result = await exporter.Export(filePath, data);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();

            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                //检查转换结果
                var sheet = pck.Workbook.Worksheets.First();
                //验证Alt
                sheet.Cells["G6"].Value.ShouldBe("404");
                //验证图片
                sheet.Drawings.Count.ShouldBe(9);
                foreach (ExcelPicture item in sheet.Drawings)
                {
                    //检查图片位置
                    new int[] { 2, 6 }.ShouldContain(item.From.Column);
                    item.ShouldNotBeNull();
                }
                sheet.Tables.Count.ShouldBe(0);
            }
        }
        #endregion
        [Fact(DisplayName = "数据注解导出测试")]
        public async Task ExportTestDataAnnotations_Test()
        {
            IExporter exporter=new ExcelExporter();
            var filePath = GetTestFilePath($"{nameof(ExportTestDataAnnotations_Test)}.xlsx");
            DeleteFile(filePath);
            var result = await exporter.Export(filePath,
                GenFu.GenFu.ListOf<ExportTestDataAnnotations>());
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
            using (var pck = new ExcelPackage(new FileInfo(filePath)))
            {
                pck.Workbook.Worksheets.Count.ShouldBe(1);
                var sheet = pck.Workbook.Worksheets.First();

                sheet.Cells["C2"].Text.Equals(DateTime.Parse(sheet.Cells["C2"].Text).ToString("yyyy-MM-dd"));
                
                sheet.Cells["D2"].Text.Equals(DateTime.Parse(sheet.Cells["D2"].Text).ToString("yyyy-MM-dd"));
                sheet.Tables.Count.ShouldBe(1);
                var tb = sheet.Tables.First();

                tb.Columns[0].Name.ShouldBe("Custom列1");
                tb.Columns[1].Name.ShouldBe("列2");
                tb.Columns.Count.ShouldBe(4);
            }
        }

        [Fact(DisplayName = "样式错误测试" )]
        public async Task TenExport_Test()
        {
            IExporter exporter = new ExcelExporter();

            var filePath = GetTestFilePath($"{nameof(TenExport_Test)}.xlsx");

            DeleteFile(filePath);

            var data = GenFu.GenFu.ListOf<GalleryLineExportModel>(100);

            var result = await exporter.Export(filePath, data);
            result.ShouldNotBeNull();
            File.Exists(filePath).ShouldBeTrue();
     
        }
    }
}