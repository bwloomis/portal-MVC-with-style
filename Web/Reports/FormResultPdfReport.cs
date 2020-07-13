using Data.Abstract;
using PdfFileWriter;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;


namespace Assmnts.Reports
{

	public class FormResultPdfReport
	{
		protected static readonly double labelIndent = 1;
		protected static readonly double valueIndent = 4;

		protected readonly IFormsRepository formsRepo;
		protected readonly int formResultId;
		protected readonly def_Forms form;
		protected readonly def_FormResults formResults;
		protected readonly PdfOutput output;

		private List<string> customSectionOrderByIdentifiers = null;
		private string sectionIdentifierPrefixToRemove = null;

		/// <summary>
		/// Prints generic report of Assessment Questions / Answers
		/// </summary>
		/// <param name="formsRepo">Forms Repository</param>
		/// <param name="formResultId">Form Result ID (Assessment Answers)</param>
		/// <param name="outputPath"></param>
		/// <param name="grayscale"></param>
		/// <param name="customSectionOrderByIdentifiers"></param>
		public FormResultPdfReport(
				IFormsRepository formsRepo,
				int formResultId,
				string outputPath,
				bool grayscale)
		{
			Debug.WriteLine("FormResultPdfReport formResultId: " + formResultId.ToString());
			this.formResultId = formResultId;
			this.formsRepo = formsRepo;
			this.formResults = (formResultId == -1) ? null : formsRepo.GetFormResultById(formResultId);
			this.form = (formResults == null) ? null : formsRepo.GetFormById(formResults.formId);//formResults.def_Forms;
			this.output = new PdfOutput(grayscale, outputPath);

			//add page number 1 as footer on first page
			output.appendPageFooter();
		}

		public void setCustomSectionOrder(params string[] sectionIdentifiers)
		{
			customSectionOrderByIdentifiers = new List<string>(sectionIdentifiers);
		}

        public void defaultOrderSections()
        {
            if (customSectionOrderByIdentifiers == null)
            {
                customSectionOrderByIdentifiers = new List<string>();
            }

            List<def_FormParts> formParts = formsRepo.GetFormPartsByFormId(form.formId).OrderBy(fp => fp.order).Select(fp => fp).ToList();
            foreach (var fp in formParts) {
                def_Parts part = formsRepo.GetPartById(fp.partId);
                List<def_Sections> sections = formsRepo.GetSectionsInPart(part);
                foreach(var s in sections) {
                    customSectionOrderByIdentifiers.Add(s.identifier);
                }
            }
        }

		public void setSectionIdentifierPrefixToRemove(string prefix)
		{
			sectionIdentifierPrefixToRemove = prefix;
		}

		public void setFontSize(double fontSize)
		{
			output.fontSize = fontSize;
		}

		public void setPartHeaderFontSize(double size)
		{
			output.partHeaderFontSize = size;
		}

		public void setSectionHeaderFontSize(double size)
		{
			output.sectionHeaderFontSize = size;
		}

		virtual public void BuildReport()
		{
			var formParts = formsRepo.GetFormPartsByFormId(formResults.def_Forms.formId);
			foreach (def_FormParts fp in formParts) {
				var part = formsRepo.GetPartById(fp.partId);
				AppendPartHeader(output, part.identifier);
				List<def_Sections> sectionsInPart = formsRepo.GetSectionsInPart(part);
				List<def_Sections> sections = getCustomOrderedSections(sectionsInPart);
				foreach (def_Sections sct in sections) {
					Debug.WriteLine("BuildReport PrintGenericSection: " + sct.identifier);
					PrintGenericSection(output, sct, 1);
				}
			}
		}

		private List<def_Sections> getCustomOrderedSections(ICollection<def_Sections> original)
		{
			List<def_Sections> result = new List<def_Sections>(original);
			if (customSectionOrderByIdentifiers != null) {
				result.Sort(delegate (def_Sections a, def_Sections b) {
					return getCustomOrder(a).CompareTo(getCustomOrder(b));
				});
			}
			return result;
		}

		private int getCustomOrder(def_Sections sct)
		{
			if (customSectionOrderByIdentifiers.Contains(sct.identifier))
				return customSectionOrderByIdentifiers.IndexOf(sct.identifier);
			return -1;
		}

		protected string buildSectionHeaderText(def_Sections section)
		{
			string fixedIdentifier;

			if (sectionIdentifierPrefixToRemove != null && section.identifier.StartsWith(sectionIdentifierPrefixToRemove))
				fixedIdentifier = section.identifier.Substring(sectionIdentifierPrefixToRemove.Length);
			else
				fixedIdentifier = section.identifier;

			return "(" + fixedIdentifier + ") " + section.title;
		}

		//* * * OT 2-26-16 this should be refactored so that all the responses for the formResult are pulled once, all at one time, using a CommonExport function
		protected Dictionary<string, string> GetResponsesByItemVariableIdentifier(def_Sections sct)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();
			foreach (def_Items itm in formsRepo.GetSectionItems(sct)) {
				formsRepo.GetItemVariables(itm);
				foreach (def_ItemVariables iv in itm.def_ItemVariables) {
					if (result.ContainsKey(iv.identifier))
						continue;

					result.Add(iv.identifier, GetResponse(formResultId, itm, iv));
				}
			}

			Debug.WriteLine("GetResponsesByItemVariableIdentifier: " + sct.identifier + "  result.Count: " + result.Count.ToString());
			return result;
		}


		protected virtual void PrintGenericSection(PdfOutput output, def_Sections section, int indentLevel)
		{
			//pull out all responses for items in this section (doesn't include items in subsections)
			Debug.WriteLine("PrintGenericSection section: " + section.identifier);
			Dictionary<string, string> responsesByItemVariable = GetResponsesByItemVariableIdentifier(section);

			PrintGenericSection(output, section, indentLevel, responsesByItemVariable);
		}

		protected void PrintGenericSection(
						PdfOutput output,
						def_Sections section,
						int indentLevel,
						Dictionary<string, string> responsesByItemVariable)
		{
			if (output.drawY < 1.5)
				output.appendPageBreak();

			if (indentLevel < 2)
				output.appendSectionBreak();

			//print section title + identifier
			double sectionLabelIndent = .5 + labelIndent * (indentLevel - 1);
			double itemLabelIndent = labelIndent * indentLevel;
			double responseIndent = valueIndent + labelIndent * (indentLevel - 1);
			output.appendWrappedText(buildSectionHeaderText(section),
					sectionLabelIndent, 8 - sectionLabelIndent, output.sectionHeaderFontSize);
			output.drawY -= .1;

			List<def_Items> ignoreList = new List<def_Items>();
			int singleBoolCount = 0;
			formsRepo.GetSectionItems(section);

			//add items wtihout responses to the ignore list, count the number of single-boolean items
			Debug.WriteLine("FormResultPdfReport.PrintGenericSection section: " + section.identifier);
			foreach (def_SectionItems si in section.def_SectionItems.Where(si => !si.subSectionId.HasValue)) {
				def_Items itm = formsRepo.GetItemById(si.itemId);
				Debug.WriteLine("   itm: " + itm.identifier);
				ICollection<def_ItemVariables> ivs = formsRepo.GetItemVariablesByItemId(itm.itemId);
				if (ivs.Any(iv => !responsesByItemVariable.ContainsKey(iv.identifier) || String.IsNullOrWhiteSpace(responsesByItemVariable[iv.identifier])))
					ignoreList.Add(itm);

				if ((ivs.Count == 1) && (ivs.First().baseTypeId == 1))
					singleBoolCount++;
			}

			//if there at least 4 boolean items in this section, ignore all labeled items with single negative boolean responses
			if (singleBoolCount >= 4) {
				foreach (def_SectionItems si in section.def_SectionItems.Where(si => !si.subSectionId.HasValue)) {
					def_Items itm = formsRepo.GetItemById(si.itemId);
					ICollection<def_ItemVariables> ivs = formsRepo.GetItemVariablesByItemId(itm.itemId);
					if (ignoreList.Contains(itm) || itm.label.Trim().Length == 0)
						continue;

					if (ivs.Count == 1) {
						def_ItemVariables iv = ivs.First();
						if ((iv.baseTypeId == 1) && (!responsesByItemVariable.ContainsKey(iv.identifier) || responsesByItemVariable[iv.identifier].Equals("No")))
							ignoreList.Add(itm);
					}
				}
			}

			//iterate through section items, printing to pdf output
			foreach (def_SectionItems si in section.def_SectionItems) {
				if (si.subSectionId.HasValue) {
					PrintGenericSection(output, formsRepo.GetSubSectionById(si.subSectionId.Value), indentLevel + 1);
				} else {
					def_Items itm = formsRepo.GetItemById(si.itemId);
					if (ignoreList.Where(x => x.itemId == itm.itemId).Any()) {
						continue;
					}

					formsRepo.GetEnterpriseItems(new def_Items[] { itm }.ToList(), formResults.EnterpriseID.Value);
					appendItemLabelAndResponses(itm, itemLabelIndent, responseIndent, responsesByItemVariable);
				}
			}
		}

		//if responsesByItemVariable is supplied, responses will be pulled from there rather than db
		protected void appendItemLabelAndResponses(
				def_Items itm,
				double itemLabelIndent,
				double responseIndent,
				Dictionary<string, string> responsesByItemVariableIdentifier = null)
		{
			List<string> responses = new List<string>();
			List<def_ItemVariables> itemVariables = formsRepo.GetItemVariablesByItemId(itm.itemId);
			foreach (def_ItemVariables iv in itemVariables) {
				//if this itemvariable has been explicitely removed from responsesByItemVariableIdentifier, skip it
				if ((responsesByItemVariableIdentifier != null) && !responsesByItemVariableIdentifier.ContainsKey(iv.identifier))
					continue;

				string rsp = (responsesByItemVariableIdentifier == null) ? GetSingleResponse(iv.identifier) : responsesByItemVariableIdentifier[iv.identifier];
				if (rsp != null)
					responses.Add(rsp);
			}

			//if there are no responses, terminate without even printing the item label
			if (responses.Count == 0)
				return;

			//append item label, without actually moving drawY down the page
			double itemLabelHeight = output.appendWrappedText(itm.label, itemLabelIndent, responseIndent - itemLabelIndent, output.boldFont);
			double drawYBelowItemLabel = output.drawY;
			output.drawY += itemLabelHeight;

			//append responses to the right of the item label
			foreach (string rsp in responses)
				output.appendWrappedText(rsp, responseIndent, 8 - responseIndent);

			output.drawY = Math.Min(drawYBelowItemLabel, output.drawY - .05);
		}

		public void outputToFile()
		{
			output.outputToFile();
		}

		virtual protected void AppendPartHeader(PdfOutput output, string title)
		{
			output.drawY -= .5;
			output.DrawText(output.boldFont, output.partHeaderFontSize, PdfOutput.pageWidth / 2, output.drawY, TextJustify.Center, title);
			output.SetColor(output.sisred);
			output.OutlineRectangle(PdfOutput.firstColumnHeaderX, output.drawY - .05,
					PdfOutput.pageWidth - PdfOutput.firstColumnHeaderX - PdfOutput.rightMargin, .3);
			output.FillRectangle(PdfOutput.firstColumnHeaderX + .05, output.drawY - .08,
					PdfOutput.pageWidth - PdfOutput.firstColumnHeaderX - PdfOutput.rightMargin, .03);
			output.FillRectangle(PdfOutput.firstColumnHeaderX +
					(PdfOutput.pageWidth - PdfOutput.firstColumnHeaderX - PdfOutput.rightMargin), output.drawY - .05, .05, .28);
			output.SetColor(Color.Black);
			output.drawY -= .20;
			output.drawY -= PdfOutput.itemSpacing;
		}

		#region layout helpers
		protected void buildTableWithItems(PdfOutput output, def_Parts part, int nColumns, params string[] identifiers)
		{
			string[] headers = new string[nColumns];
			for (int i = 0; i < nColumns; i++) {
				string ident = identifiers[i];
				def_Items itm = formsRepo.GetItemByIdentifier(/*formResults,*/ ident);//formsRepo.GetAllItems().First(t => t.identifier.Equals(ident));//getItemByIdentifier(part,ident);
				if (itm == null) {
					throw new Exception("could not find item with identifer " + ident);
				}
				headers[i] = itm.label.Replace("*", "");
			}
			buildTableWithItems(output, part, nColumns, headers, identifiers);
		}

		protected void buildTableWithItems(PdfOutput output, def_Parts part, int nColumns, string[] headers, params string[] identifiers)
		{
			int nRows = identifiers.Length / nColumns;
			string[][] vals = new string[nRows][];
			for (int row = 0; row < nRows; row++) {
				vals[row] = new string[nColumns];
				for (int col = 0; col < nColumns; col++) {
					string ident = identifiers[row * nColumns + col];
					def_Items itm = formsRepo.GetItemByIdentifier(/*formResults,*/ ident);//formsRepo.GetAllItems().First(t => t.identifier.Equals(ident));//getItemByIdentifier(part,ident);
					if (itm == null)
						throw new Exception("could not find item with identifer " + ident);
					formsRepo.GetItemVariables(itm);
					def_ItemVariables iv = itm.def_ItemVariables.FirstOrDefault();
					if (iv == null)
						throw new Exception("could not find any itemVariables for item with identifer " + ident);
					def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);//iv.def_ResponseVariables.FirstOrDefault();
					vals[row][col] = ((rv == null) ? "" : rv.rspValue);
				}
			}
			output.appendSimpleTable(headers, vals);
		}

		protected void buildSubheaderWithResults(PdfOutput output, def_Parts part, string subheader, params object[] identifiersOrPairs)
		{
			output.appendSubHeader(subheader);
			BuildItemResults(output, part, identifiersOrPairs);
		}

		//a label -> value pair, which can be used as an alternative to an item variable identifier
		protected class LabelValuePair
		{
			public readonly string label, value;

			public LabelValuePair(string label, string value)
			{
				this.label = label;
				this.value = value;
			}
		}

		//                BuildItemResults(output, part, "SIS-Prof1_PageNotes_item");

		protected void BuildItemResults(PdfOutput output, def_Parts part, params object[] identifiersOrPairs)
		{
			foreach (object identOrPair in identifiersOrPairs) {
				if (identOrPair is string) {
					string ident = (string)identOrPair;
					def_Items itm = formsRepo.GetItemByIdentifier(ident);
					formsRepo.GetEnterpriseItems(new def_Items[] { itm }.ToList(), formResults.EnterpriseID.Value);
					if (itm == null)
						throw new Exception("could not find item with identifier " + ident);
					string rv = GetSingleResponse(itm);
					output.appendItem(itm.label.Replace("*", ""), (rv == null) ? "" : rv.Replace("@", "@    "));
				} else if (identOrPair is LabelValuePair) {
					LabelValuePair p = (LabelValuePair)identOrPair;
					output.appendItem(p.label, p.value);
				} else {
					throw new Exception("unrecognized object type in parameter \"identifieresOrPairs\", objects must be of type string or pair");
				}
			}
		}
		#endregion

		#region data-access helpers

		protected string GetResponse(int formResultId, def_Items itm, def_ItemVariables iv)
		{
			def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);
			string response = (rv == null) ? String.Empty : rv.rspValue;

			// special case for drop-downs/radiolists with options in meta-data
			if ((itm != null) && (iv.baseTypeId == 8) && !String.IsNullOrWhiteSpace(itm.prompt) && itm.prompt.Contains(";")) {
				try {
					return itm.prompt.Split(';')[Convert.ToInt16(response) - 1];
				}
				catch (Exception e) {
					Debug.WriteLine(" *** FormResultPdfReport.cs failed to retrieve selected option for ItemVariable "
							+ iv.itemVariableId + ", response string \"" + response + "\"");
				}
			}

			//special case for YES/NO items
			else if ((response != null) && (iv.baseTypeId == 1)) {
				if (response.Equals("1"))
					return "Yes";

				if (response.Equals("0"))
					return "No";
			}

			return response;
		}

		protected string GetSingleResponse(def_Items itm)
		{
			def_ItemResults ir = formsRepo.GetItemResultByFormResItem(formResultId, itm.itemId);
			if (ir == null) {
				return String.Empty;
			}

			formsRepo.GetItemResultsResponseVariables(ir);
			def_ResponseVariables rv = ir.def_ResponseVariables.FirstOrDefault();
			return (rv == null) ? String.Empty : rv.rspValue;
		}

		protected string GetSingleResponse(string ivIdentifier)
		{
			def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, ivIdentifier);
			return (rv == null) ? null : rv.rspValue;
		}
		#endregion
	}
}
