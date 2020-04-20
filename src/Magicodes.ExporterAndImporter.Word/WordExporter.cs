﻿// ======================================================================
//
//           filename : WordExporter.cs
//           description :
//
//           created by 雪雁 at  2019-09-26 14:59
//           文档官网：https://docs.xin-lai.com
//           公众号教程：麦扣聊技术
//           QQ群：85318032（编程交流）
//           Blog：http://www.cnblogs.com/codelove/
//
// ======================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Core.Models;
using Magicodes.ExporterAndImporter.Html;

namespace Magicodes.ExporterAndImporter.Word
{
	/// <summary>
	///     Word导出
	/// </summary>
	public class WordExporter : IExportListFileByTemplate, IExportFileByTemplate
	{
		/// <summary>
		///     根据模板导出列表
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fileName"></param>
		/// <param name="dataItems"></param>
		/// <param name="htmlTemplate"></param>
		/// <returns></returns>
		public async Task<ExportFileInfo> ExportListByTemplate<T>(string fileName, ICollection<T> dataItems,
			string htmlTemplate = null) where T : class
		{
			if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("文件名必须填写!", nameof(fileName));
			var exporter = new HtmlExporter();
			var htmlString = await exporter.ExportListByTemplate(dataItems, htmlTemplate);

			using (var generatedDocument = new MemoryStream())
			{
				using (var package =
					WordprocessingDocument.Create(generatedDocument, WordprocessingDocumentType.Document))
				{
					var mainPart = package.MainDocumentPart;
					if (mainPart == null)
					{
						mainPart = package.AddMainDocumentPart();
						new Document(new Body()).Save(mainPart);
					}

					var converter = new HtmlConverter(mainPart);
					converter.ParseHtml(htmlString);

					mainPart.Document.Save();
				}

				File.WriteAllBytes(fileName, generatedDocument.ToArray());
				var fileInfo = new ExportFileInfo(fileName,
					"application/vnd.openxmlformats-officedocument.wordprocessingml.document");
				return fileInfo;
			}
		}

		/// <summary>
		///     根据模板导出
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fileName"></param>
		/// <param name="data"></param>
		/// <param name="htmlTemplate"></param>
		/// <returns></returns>
		public async Task<ExportFileInfo> ExportByTemplate<T>(string fileName, T data, string htmlTemplate)
			where T : class
		{
			if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("文件名必须填写!", nameof(fileName));
			var result = await ExportBytesByTemplate(data, htmlTemplate);

			File.WriteAllBytes(fileName, result);
			var fileInfo = new ExportFileInfo(fileName,
				"application/vnd.openxmlformats-officedocument.wordprocessingml.document");
			return fileInfo;
		}

		/// <summary>
		///   根据模板导出bytes
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		/// <param name="template"></param>
		/// <returns></returns>

		public async Task<byte[]> ExportBytesByTemplate<T>(T data, string template) where T : class
		{
			var exporter = new HtmlExporter();
			var htmlString = await exporter.ExportByTemplate(data, template);

			using (var generatedDocument = new MemoryStream())
			{
				using (var package =
					WordprocessingDocument.Create(generatedDocument, WordprocessingDocumentType.Document))
				{
					var mainPart = package.MainDocumentPart;
					if (mainPart == null)
					{
						mainPart = package.AddMainDocumentPart();
						new Document(new Body()).Save(mainPart);
					}

					var converter = new HtmlConverter(mainPart);
					converter.ParseHtml(htmlString);

					mainPart.Document.Save();
					return generatedDocument.ToArray();
				}
			}
		}
        /// <summary>
		///   根据模板导出bytes
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		/// <param name="template"></param>
		/// <returns></returns>

		public async Task<byte[]> ExportListBytesByTemplate<T>(ICollection<T> data, string template) where T : class
        {
            var exporter = new HtmlExporter();
            var htmlString = await exporter.ExportListByTemplate(data, template);

            using (var generatedDocument = new MemoryStream())
            {
                using (var package =
                    WordprocessingDocument.Create(generatedDocument, WordprocessingDocumentType.Document))
                {
                    var mainPart = package.MainDocumentPart;
                    if (mainPart == null)
                    {
                        mainPart = package.AddMainDocumentPart();
                        new Document(new Body()).Save(mainPart);
                    }

                    var converter = new HtmlConverter(mainPart);
                    converter.ParseHtml(htmlString);

                    mainPart.Document.Save();
                    return generatedDocument.ToArray();
                }
            }
        }
	}
}