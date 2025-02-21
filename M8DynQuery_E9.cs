using System.Collections.Generic;
using Epicor.Mfg.Core;
using Infragistics.Win.UltraWinGrid;
using Infragistics.Win;
 
class M8DynQuery_E9
{
    private string baqName;
	private string pBaqName;
    private DynamicQuery dqBO;
    private QueryDesignDataSet qds; // Use QueryDesignDataSet instead of QueryExecutionDataSet
	private QueryExecutionDataSet execParams;
    private EpiDataView edv;
    private DataTable results;
    private EpiUltraGrid grid;
    private Dictionary<string, string> baqParams;
    private Dictionary<string, string> defParams;
    private Dictionary<string, string> lastParams;
    private bool changedParams;
    private EpiTransaction oTrans;
    private Control mainControl;
    private bool gotBAQ;
	private string[] keynames;
	private string[] keys;
	bool autoselect = true;
	bool defaulttolastrow = false;
	private Dictionary<string, Control> filterControls;
 
    public M8DynQuery_E9(string baqname, EpiTransaction trans, Control sheet, EpiUltraGrid ultragrid, string[] paramnames, string[] paramdefaults, string[] keyNames, string[] colsToShow)
    {
        baqName = baqname;
		pBaqName = baqname.Replace("-","").Replace("_","");
        oTrans = trans;
 
        dqBO = new DynamicQuery(((Session)oTrans.Session).ConnectionPool);
 
        edv = new EpiDataView();
        results = new DataTable();
        edv.dataView = results.DefaultView;
        oTrans.Add(baqName, edv);
        changedParams = true;
        mainControl = sheet;
        gotBAQ = false;
 
        if (paramnames != null && paramdefaults != null && paramnames.Length == paramdefaults.Length)
        {
            baqParams = new Dictionary<string, string>();
            defParams = new Dictionary<string, string>();
            for (int i = 0; i < paramnames.Length; i++)
            {
                baqParams[paramnames[i]] = paramdefaults[i];
                defParams[paramnames[i]] = paramdefaults[i];
            }
        }
		keynames = (string[])keyNames.Clone();
		keys = new string[keyNames.Length];
		//edv.dataView.RowChanged += edv_RowChanged;
 
        if (ultragrid != null)
        {
            grid = ultragrid;
            grid.EpiBinding = edv.ViewName;
            grid.UpdateMode = UpdateMode.OnCellChange;
			//grid.UseOsThemes = DefaultableBoolean.False;
			grid.StyleSetName = string.Empty;
			grid.AfterRowActivate += new System.EventHandler(grid_AfterRowActivate);
			if (colsToShow != null)
			{
				UltraGridBand band = grid.DisplayLayout.Bands[0];
				for (int i = 0; i < band.Columns.Count; i++)
				{
					if (Array.IndexOf(colsToShow,band.Columns[i].Key) > -1)
					{
						band.Columns[i].Hidden = false;
					}
					else
					{
						band.Columns[i].Hidden = true;
					}
				}
			}
        }
        GetData(false);
		if (grid != null)
		{
			if (colsToShow != null)
			{
				UltraGridBand band = grid.DisplayLayout.Bands[0];
				for (int i = 0; i < band.Columns.Count; i++)
				{
					if (Array.IndexOf(colsToShow,band.Columns[i].Key) > -1)
					{
						band.Columns[i].Hidden = false;
					}
					else
					{
						band.Columns[i].Hidden = true;
					}
				}
			}
		}
		EpiBaseForm parentForm = (EpiBaseForm)oTrans.EpiBaseForm;
		if (parentForm != null)
		{
			parentForm.WindowState = FormWindowState.Normal;
			parentForm.Width = 1290;
			parentForm.Height = 800;
		}
		filterControls = new Dictionary<string, Control>(); 
		FindFilterControls();
    }

	public void FindFilterControls()  
    {  
        Control top = grid;  
        while (top.Parent != null) { top = top.Parent; }  
        AddFilterControl(top);  
    }  

    private void AddFilterControl(Control parentcontrol)  
    {  
        foreach (Control c in parentcontrol.Controls)  
        {  
            if (c.HasChildren)  
            {  
                AddFilterControl(c);  
            }  
            else  
            {  
                if (c.Tag != null && c.Tag.ToString() != string.Empty)  
                {  
                    string tag = c.Tag.ToString(); 
                    string[] bits = tag.Split(' ');  
                    for (int i = 0; i < bits.Length; i++)  
                    {  
                        if (bits[i].Length > 1 && bits[i].Substring(0,2) == "f:")  
                        {  
                            string[] fbits = bits[i].Substring(2, bits[i].Length - 2).Split('.');  
							if (fbits.Length > 2)
							{
								for (int j = 2; j < fbits.Length; j++)
								{
									fbits[1] += "." + fbits[j];
								}
								string[] nfbits = new string[2];
								Array.Copy(fbits,nfbits,2);
								fbits = nfbits;
							}
                            if (fbits.Length == 2 && fbits[0] == baqName)  
                            {  
                                //MessageBox.Show(c.Name + " " + fbits[1]);  
                                if (filterControls == null) { filterControls = new Dictionary<string, Control>(); }  
                                filterControls[fbits[1]] = c;  
                                if (c is EpiTextBox)  
                                {  
                                    ((EpiTextBox)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is EpiCombo)  
                                {  
                                    ((EpiCombo)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is EpiCheckBox)  
                                {  
                                    ((EpiCheckBox)c).CheckedChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is BAQCombo)  
                                {  
                                    ((BAQCombo)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is EpiDateTimeEditor)  
                                {  
                                    ((EpiDateTimeEditor)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is EpiTimeEditor)  
                                {  
                                    ((EpiTimeEditor)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is EpiNumericEditor)  
                                {  
                                    ((EpiNumericEditor)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is EpiCurrencyEditor)  
                                {  
                                    ((EpiCurrencyEditor)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                                else if (c is EpiRetrieverCombo)  
                                {  
                                    ((EpiRetrieverCombo)c).ValueChanged += new System.EventHandler(FilterControl_ValueChanged);  
                                }  
                            }  
                        }  
                    }  
                }  
            }  
        }  
    }  

	private void FilterControl_ValueChanged(object sender, System.EventArgs args)  
    {  
        FilterGrid();  
        //MessageBox.Show(((Control)sender).Name);  
    }  

    private FilterComparisionOperator FilterComp(string strcomp, ref string repchars)  
    {  
        FilterComparisionOperator comp = FilterComparisionOperator.Equals;  
        switch (strcomp)  
        {  
            case "EQUALS":  
                comp = FilterComparisionOperator.Equals;  
                break;  
            case "CONTAINS":  
                comp = FilterComparisionOperator.Contains;  
                break;  
            case "GREATERTHAN":  
                comp = FilterComparisionOperator.GreaterThan;  
                break;  
            case "LESSTHAN":  
                comp = FilterComparisionOperator.LessThan;  
                break;  
            case "GREATERTHANOREQUALTO":  
                comp = FilterComparisionOperator.GreaterThanOrEqualTo;  
                break;  
            case "LESSTHANOREQUALTO":  
                comp = FilterComparisionOperator.LessThanOrEqualTo;  
                break;  
            case "STARTSWITH":  
                comp = FilterComparisionOperator.StartsWith;  
                break;  
            case "LIKE":  
                comp = FilterComparisionOperator.Like;  
                repchars = "*";  
                break;  
            case "NOTEQUALS":  
                comp = FilterComparisionOperator.NotEquals;  
                break;  
            case "ENDSWITH":  
                comp = FilterComparisionOperator.EndsWith;  
                break;  
            case "DOESNOTCONTAIN":  
                comp = FilterComparisionOperator.DoesNotContain;  
                break;  
            case "MATCH":  
                comp = FilterComparisionOperator.Match;  
                repchars = ".*";  
                break;  
            case "DOESNOTMATCH":  
                comp = FilterComparisionOperator.DoesNotMatch;  
                repchars = ".*";  
                break;  
            case "NOTLIKE":  
                comp = FilterComparisionOperator.NotLike;  
                repchars = "*";  
                break;  
            case "DOESNOTSTARTWITH":  
                comp = FilterComparisionOperator.DoesNotStartWith;  
                break;  
            case "DOESNOTENDWITH":  
                comp = FilterComparisionOperator.DoesNotEndWith;  
                break;  
        }  
        return comp;  
    }  

    private void FilterWorkings(ref string val, ref string[] valbits, string[] keybits, ref FilterComparisionOperator comp, ref FilterLogicalOperator op, ref string repchars)  
    {  
        comp = FilterComparisionOperator.Equals;  
        op = FilterLogicalOperator.And;  
        string strcomp = string.Empty;  
        if (keybits.Length > 1)  
        {  
            strcomp = keybits[1].ToUpper();  
            comp = FilterComp(strcomp, ref repchars);  
        }  
        if (keybits.Length > 2)  
        {  
            string strop = keybits[2].ToUpper();  
            switch (strop)  
            {  
                case "AND":  
                    op = FilterLogicalOperator.And;  
                    break;  
                case "OR":  
                    op = FilterLogicalOperator.Or;  
                    break;  
            }  
        }  
        if (keybits.Length > 3)  
        {  
            if (keybits[3] == "LIKEALL")  
            {  
                string[] likebits = val.Split('"');  
                List<string> keywords = new List<string>();  
                for (int i = 0; i < likebits.Length; i++)  
                {  
                    if ((i % 2) == 1)  
                    {  
                        keywords.Add(likebits[i]);  
                    }  
                    else  
                    {  
                        keywords.AddRange(likebits[i].Split(' '));  
                    }  
                }  
                valbits = keywords.ToArray();  
            }  
        }  
        if (strcomp == "MATCH")  
        {  
            List<string> matchchars = new List<string>( new string[] {" ", ".", "-", ","} );  
            string replacechars = "[ ,.-]?";  
            foreach (string matchchar in matchchars)  
            {  
                val = val.Replace(matchchar, "~~");  
            }  
                val = val.Replace("~~", replacechars);  
        }  
        else if (repchars != " ") { val = repchars + val + repchars; }  
    }  

    public void FilterGrid()  
    {  
        if (grid != null)  
        {  
            UltraGridBand band = grid.DisplayLayout.Bands[0];  
            band.Override.RowFilterMode = RowFilterMode.AllRowsInBand;  
            foreach (ColumnFilter f in band.ColumnFilters)  
            {  
                f.FilterConditions.Clear();  
            }  
            foreach (KeyValuePair<string, Control> p in filterControls)  
            {  
                Control c = p.Value;  
                string val = ControlValue(c);  
                string[] valbits = null;  
                string[] bits = p.Key.Split('~');  
                string key = bits[0];  
                string repchars = " ";  
                if (val != string.Empty)  
                {  
                    FilterComparisionOperator comp = FilterComparisionOperator.Equals;  
                    FilterLogicalOperator op = FilterLogicalOperator.And;  
                    FilterWorkings(ref val, ref valbits, bits, ref comp, ref op, ref repchars);  
                    band.Columns[key].AllowRowFiltering = DefaultableBoolean.False;  
                    if (c is EpiTextBox)  
                    {  
                        if (valbits != null)  
                        {  
                            for (int i = 0; i < valbits.Length; i++)  
                            {  
                                if (repchars != " ") { valbits[i] = repchars + valbits[i] + repchars; }  
                                band.ColumnFilters[key].FilterConditions.Add(comp, valbits[i]);  
                            }  
                        }  
                        else  
                        {  
                            band.ColumnFilters[key].FilterConditions.Add(comp, val);  
                        }  
                            band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                    else if (c is EpiCombo && ((EpiCombo)c).Value != null)  
                    {  
                        band.ColumnFilters[key].FilterConditions.Add(comp, ((EpiCombo)c).Value);  
                        band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                    else if (c is EpiCheckBox && ((EpiCheckBox)c).CheckState != CheckState.Indeterminate)  
                    {  
                        band.ColumnFilters[key].FilterConditions.Add(comp, ((EpiCheckBox)c).Checked);  
                        band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                    else if (c is BAQCombo && ((BAQCombo)c).Value != null)  
                    {  
                        band.ColumnFilters[key].FilterConditions.Add(comp, ((BAQCombo)c).Value);  
                        band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                    else if (c is EpiDateTimeEditor && ((EpiDateTimeEditor)c).Value != null)  
                    {  
                        band.ColumnFilters[key].FilterConditions.Add(comp, ((DateTime)((EpiDateTimeEditor)c).Value));  
                        band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                    else if (c is EpiTimeEditor)  
                    {  
                    }  
                    else if (c is EpiNumericEditor && ((EpiNumericEditor)c).Value != null)  
                    {  
                        band.ColumnFilters[key].FilterConditions.Add(comp, ((EpiNumericEditor)c).Value);  
                        band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                    else if (c is EpiCurrencyEditor && (decimal?)((EpiCurrencyEditor)c).Value != null)  
                    {  
                        band.ColumnFilters[key].FilterConditions.Add(comp, ((EpiCurrencyEditor)c).Value);  
                        band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                    else if (c is EpiRetrieverCombo && ((EpiRetrieverCombo)c).Value != null)  
                    {  
                        band.ColumnFilters[key].FilterConditions.Add(comp, ((EpiRetrieverCombo)c).Value);  
                        band.ColumnFilters[key].LogicalOperator = op;  
                    }  
                }  
            } 
            FilterEventArgs args = new FilterEventArgs();  
            OnFilteredChange(args);  
        }  
    }  

    public void ResetFilters()  
    {  
        Control top = grid;  
        while (top.Parent != null) { top = top.Parent; }  
        foreach (KeyValuePair<string, Control> p in filterControls)  
        {  
            Control c = p.Value;  
            string val = string.Empty;  
            if (c.Tag != null && c.Tag.ToString() != string.Empty)  
            {  
                string tag = c.Tag.ToString();  
                string[] bits = tag.Split(' ');  
                for (int i = 0; i < bits.Length; i++)  
                {  
                    if (bits[i].Length > 1 && bits[i].Substring(0,2) == "p:")  
                    {  
                        string[] fbits = bits[i].Substring(2, bits[i].Length - 2).Split('.');  
                        if (fbits.Length == 2 && fbits[0] == baqName)  
                        {  
                            string[] parambits = fbits[1].Split('~');  
                            val = defParams[parambits[0]] ?? string.Empty;  
                        }  
                        break;  
                    }  
                }  
            }  
            if (c is EpiTextBox)  
            {  
                ((EpiTextBox)c).Text = val;  
            }  
            else if (c is EpiCombo)  
            {  
                ((EpiCombo)c).Value = val;  
            }  
            else if (c is EpiCheckBox)  
            {  
                if (val == string.Empty)  
                {  
                    ((EpiCheckBox)c).CheckState = CheckState.Indeterminate;  
                }  
                else  
                {  
                    bool boolval = false;  
                    if (Boolean.TryParse(val, out boolval))  
                    {  
                        ((EpiCheckBox)c).Checked = boolval;  
                    }  
                }  
            }  
            else if (c is BAQCombo)  
            {  
                ((BAQCombo)c).Value = val;  
            }  
            else if (c is EpiDateTimeEditor)  
            {  
                if (val == string.Empty)  
                {  
                    ((EpiDateTimeEditor)c).Value = null;  
                }  
                else  
                {  
                    DateTime d = DateTime.Now.Date;  
                    if (DateTime.TryParse(val, out d))  
                    {  
                        ((EpiDateTimeEditor)c).Value = d;  
                    }  
                }  
            }  
            else if (c is EpiTimeEditor)  
            {  
            }  
            else if (c is EpiNumericEditor)  
            {  
                if (val == string.Empty)  
                {  
                    ((EpiNumericEditor)c).Value = null;  
                }  
                else  
                {  
                    double d = 0.0;  
                    if (Double.TryParse(val, out d))  
                    {  
                        ((EpiNumericEditor)c).Value = d;  
                    }  
                }  
            }  
            else if (c is EpiCurrencyEditor)  
            {  
                if (val == string.Empty)  
                {  
                    ((EpiCurrencyEditor)c).Value = 0.0M;;  
                }  
                else  
                {  
                    decimal d = 0.0M;  
                    if (Decimal.TryParse(val, out d))  
                    {  
                        ((EpiCurrencyEditor)c).Value = d;  
                    }  
                }  
            }  
            else if (c is EpiRetrieverCombo)  
            {  
                ((EpiRetrieverCombo)c).Value = val;  
            }  
        }  
    } 
 
    public EpiDataView EpiDataView()
    {
        return edv;
    }
 
    public DataTable DataTable()
    {
        return results;
    }

	public DataRowView CurrentDataRow()
	{
		DataRowView row = null;
		if (edv != null && edv.Row > -1)
		{
			row = edv.dataView[edv.Row];
		}
		return row;
	}
 
	public void GetData(bool getParams)
	{
	    int ep = 0;
		try
	    {
	        oTrans.PushStatusText("Getting data for " + baqName + "...", true);
			ep = 1;
	        if (getParams && baqParams != null) { ParamsFromControls(); }
	 
	        if (string.IsNullOrEmpty(baqName))
	        {
	            return;
	        }
			ep = 2;
	        if (!gotBAQ)
	        {
	            qds = dqBO.GetByID(baqName);
	            gotBAQ = true;
	        }
	 
	        if (!gotBAQ)
	        {
	            return;
	        }
			ep = 3;
	        if (execParams == null) { execParams = dqBO.GetQueryExecutionParameters(qds); }
			ep = 4;
			DataSet queryResults;
			if (baqParams != null)
	        {
				execParams.ExecutionParameter.Clear();
				ep = 5;
	            foreach (KeyValuePair<string, string> param in baqParams)
	            {
	                QueryExecutionDataSet.ExecutionParameterRow paramRow = execParams.ExecutionParameter.NewExecutionParameterRow();
	                paramRow.ParameterName = param.Key;
	                paramRow.ParameterValue = param.Value;
	                paramRow.ValueType = "character";
	                paramRow.IsEmpty = string.IsNullOrEmpty(param.Value);
	                paramRow.RowMod = "A";
	                execParams.ExecutionParameter.AddExecutionParameterRow(paramRow);
	            }
				execParams.AcceptChanges();
				bool hasMoreRecords;
	        	queryResults = dqBO.ExecuteParametrized(qds, execParams, "", 0, out hasMoreRecords);
	        }
			else
			{
				ep = 6;
				queryResults = dqBO.ExecuteByID(baqName);
			}
			ep = 7;
	        results = queryResults.Tables["Results"];
			//MessageBox.Show(results.Rows.Count.ToString());
	        edv.dataView = results.DefaultView;
			ep = 8;
	        if (grid != null && grid.DataSource != results)
	        {
	            grid.DataSource = results;
	        }
			ep = 9;
			ApplyBAQLabelsToGrid();
			ep = 10;
			FilterEventArgs fargs = new FilterEventArgs();
			OnFilteredChange(fargs);
	        changedParams = false;
			ep = 11;
			GetDataEventArgs dargs = new GetDataEventArgs();
			dargs.GotData = true;
			OnGetData(dargs);
	    }
	    catch (Exception e)
	    {
	        MessageBox.Show("Data Error (" + ep.ToString() + ") " + baqName + " - " + e.Message);
	    }
	    finally
	    {
	        oTrans.PopStatus();
	    }
	}
 
    public void RefreshData()
    {
        bool needsRefresh = false;
        foreach (KeyValuePair<string, string> kp in baqParams)
        {
            if (!lastParams.ContainsKey(kp.Key) || lastParams[kp.Key] != kp.Value)
            {
                needsRefresh = true;
                break;
            }
        }
 
        if (needsRefresh)
        {
            GetData(true);
        }
    }
 
    public void UpdateParam(string key, string newValue)
    {
        if (baqParams.ContainsKey(key))
        {
            baqParams[key] = newValue;
            changedParams = true;
        }
    }
 
    public void ResetParams()
    {
        baqParams.Clear();
        foreach (KeyValuePair<string, string> kvp in defParams)
        {
            baqParams.Add(kvp.Key, kvp.Value);
        }
        changedParams = true;
    }
 
    private void ParamsFromControls()
    {
        Dictionary<string,string> updParams = new Dictionary<string,string>();

		foreach (KeyValuePair<string, string> kvp in baqParams)
        {
            Control[] foundControls = mainControl.Controls.Find("param" + pBaqName + kvp.Key, true);
            if (foundControls.Length > 0)
            {
				updParams[kvp.Key] = ControlValue(foundControls[0]);
            }
        }

		foreach (KeyValuePair<string,string> kvp in updParams)
		{
			UpdateParam(kvp.Key,kvp.Value);
		}
    }
 
    private string ControlValue(Control c)
    {
        if (c is EpiTextBox) return ((EpiTextBox)c).Text ?? string.Empty;
        if (c is EpiCombo) return ((EpiCombo)c).Value != null ? ((EpiCombo)c).Value.ToString() : string.Empty;
        if (c is EpiCheckBox) return ((EpiCheckBox)c).Checked.ToString();
        if (c is BAQCombo) return ((BAQCombo)c).Value != null ? ((BAQCombo)c).Value.ToString() : string.Empty;
        if (c is EpiDateTimeEditor) return ((EpiDateTimeEditor)c).Value != null ? ((DateTime)((EpiDateTimeEditor)c).Value).ToString("s") : string.Empty;
        if (c is EpiNumericEditor) return ((EpiNumericEditor)c).Value != null ? ((EpiNumericEditor)c).Value.ToString() : string.Empty;
        return string.Empty;
    }
 
    public bool Save()
    {
        bool success = true;
        try
        {
            oTrans.PushStatusText("Saving " + baqName + "...", true);
            dqBO.Update(qds); // Use qds instead of dsBAQ
 
            GetData(true);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Save Error " + baqName);
            success = false;
        }
        finally
        {
            oTrans.PopStatus();
        }
        return success;
    }

	private void ApplyBAQLabelsToGrid()
	{
	    if (grid == null || qds == null) return;
	 
	    Dictionary<string, string> fieldLabels = new Dictionary<string, string>();
	 
	    foreach (DataRow row in qds.QueryField.Rows)
	    {
	        string fieldName = Convert.ToString(row["FieldName"]); // Column key
	        string fieldLabel = Convert.ToString(row["FieldLabel"]); // Correct column label
	        if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldLabel))
	        {
	            fieldLabels[fieldName] = fieldLabel;
	        }
	    }
	 
	    foreach (UltraGridColumn column in grid.DisplayLayout.Bands[0].Columns)
	    {
			string[] bits = column.Key.Split('.');
			string key = bits[bits.Length - 1];
			if (fieldLabels.ContainsKey(key))
	        {
	            column.Header.Caption = fieldLabels[key];
	        }
	    }
		//grid.DisplayLayout.PerformAutoResizeColumns(false,PerformAutoSizeType.AllRowsInBand);
		grid.Refresh();
	}

	private UltraGridRow GoToGridRow()  
    {  
        UltraGridRow ret = null;  
        bool multi = false;  
        if ((Control.ModifierKeys == Keys.Shift || Control.ModifierKeys == Keys.Control) && grid.ContainsFocus) { multi = true; }  
        if (grid != null && keynames != null && keys != null && keynames.Length == keys.Length)  
        {  
            bool rowmatch = false;  
            int rowcount = 0;  
            if (grid.ActiveRow != null)  
            {  
                UltraGridRow row = grid.ActiveRow;  
                if (row.Hidden || row.IsFilteredOut)  
                {  
                    rowmatch = false;  
                }  
                else  
                {  
                    rowmatch = true;  
                    for (int i = 0; i < keys.Length; i++)  
                    {  
                        if (!String.Equals(row.Cells[keynames[i]].Value.ToString(), keys[i], StringComparison.OrdinalIgnoreCase))  
                        {  
                            rowmatch = false;  
                            break;  
                        }  
                    }  
                }  
                if (rowmatch && row != null)  
                {  
                    ret = row;  
                    grid.Selected.Rows.Clear();
                    grid.ActiveRow.Selected = true;  
                }  
            }  
            if (!rowmatch)  
            {  
                foreach (UltraGridRow row in grid.Rows)  
                {  
                    if (row.Hidden || row.IsFilteredOut)  
                    {  
                        rowmatch = false;  
                    }  
                    else  
                    {  
                        rowmatch = true;  
                        rowcount++;  
                        for (int i = 0; i < keys.Length; i++)  
                        {  
                            if (!String.Equals(row.Cells[keynames[i]].Value.ToString(), keys[i], StringComparison.OrdinalIgnoreCase))  
                            {  
                                rowmatch = false;  
                                break;  
                            }  
                        }  
                    }  
                    if (rowmatch && row != null)  
                    {  
                        grid.ActiveRow = row;  
                        ret = row;  
                        grid.Selected.Rows.Clear();
                        grid.ActiveRow.Selected = true;  
                        break;  
                    }  
                }  
                if (!rowmatch)  
                {  
                	grid.Selected.Rows.Clear();
                    if (rowcount == 1 && autoselect)  
                    {  
                        grid.ActiveRow = grid.Rows.GetFilteredInNonGroupByRows()[0];  
                        ret = grid.ActiveRow;  
                        for (int i = 0; i < keys.Length; i++)  
                        {  
                            keys[i] = grid.ActiveRow.Cells[keynames[i]].Value.ToString();  
                        }  
                    }  
                    else  
                    {  
                        grid.ActiveRow = null;  
                        for (int i = 0; i < keys.Length; i++)  
                        {  
                            keys[i] = string.Empty;  
                        }  
                    }  
                }  
            }  
        }  
        return ret;  
    }  
 
    private DataRowView GoToViewRow(bool newdata)  
    {  
        DataRowView ret = null;  
        bool moved = false;  
        bool newsinglerow = false;  
        string[] keycopy = (string[])keys.Clone();  
        if (edv != null && edv.dataView != null && edv.dataView.Count > 0 && keynames != null && keys != null && keynames.Length == keys.Length)  
        {  
            bool rowmatch = false;  
            if (edv.Row > -1 && edv.Row < edv.dataView.Count)  
            {  
                DataRowView row = edv.dataView[edv.Row];  
                rowmatch = true;  
                for (int i = 0; i < keys.Length; i++)  
                {  
                    if (!String.Equals(row[keynames[i]].ToString(), keys[i], StringComparison.OrdinalIgnoreCase))  
                    {  
                        rowmatch = false;  
                        break;  
                    }  
                }  
            }  
            if (!rowmatch)  
            {  
                int rowindex = 0;  
                moved = true;  
                foreach (DataRowView row in edv.dataView)  
                {  
                    rowmatch = true;  
                    for (int i = 0; i < keys.Length; i++)  
                    {  
                        if (!String.Equals(row[keynames[i]].ToString(), keys[i], StringComparison.OrdinalIgnoreCase))  
                        {  
                            rowmatch = false;  
                            break;  
                        }  
                    }  
                    if (rowmatch)  
                    {  
                        edv.Row = rowindex;  
                        break;  
                    }  
                    rowindex++;  
                }  
                if (!rowmatch)  
                {  
                    if (grid != null || edv.dataView.Count == 0)  
                    {  
                        edv.Row = -1;  
                        for (int i = 0; i < keys.Length; i++)  
                        {  
                            keys[i] = string.Empty;  
                        }  
                    }  
                    else  
                    {  
                        edv.Row = 0;  
                        for (int i = 0; i < keys.Length; i++)  
                        {  
                            if (keys[i] != edv.dataView[0][keynames[i]].ToString())  
                            {  
                                newsinglerow = true;  
                                keys[i] = edv.dataView[0][keynames[i]].ToString();  
                            }  
                        }  
                    }  
                }  
            }  
        }  
        if (edv.Row < 0 && defaulttolastrow && edv.dataView.Count > 0)  
        {  
            edv.Row = edv.dataView.Count -1;  
            for (int i = 0; i < keys.Length; i++)  
            {  
                if (keys[i] != edv.dataView[edv.Row][keynames[i]].ToString())  
                {  
                    newsinglerow = true;  
                    keys[i] = edv.dataView[edv.Row][keynames[i]].ToString();  
                }  
            }  
            GoToGridRow();  
        }  
        edv.Notify(new EpiNotifyArgs(oTrans, edv.Row, 0));  
        if (moved || newdata || newsinglerow)  
        {  
            RowEventArgs args = new RowEventArgs();  
            args.edvName = baqName;  
            args.rowindex = edv.Row;  
            args.rowchanged = ArraysEqual<string>(keys, keycopy);  
            OnRowChange(args);  
        }  
        return ret;  
    } 

	public bool GoToRow(string[] newkeys)  
    {  
        bool ret = false;  
        try  
        {  
            if (newkeys != null && keys != null && newkeys.Length == keys.Length)  
            {  
                keys = newkeys;  
                UltraGridRow grow = GoToGridRow();  
                DataRowView erow = GoToViewRow(true);  
                if (grow != null) { ret = true; }  
            }  
        }  
        catch (Exception e)  
        {  
            ret = false;  
        }  
        return ret;  
    }  
 
    private void grid_AfterRowActivate(object sender, System.EventArgs args)  
    {  
        AfterGridRowActivate(grid.ActiveRow); 
    }  
 
    public void AfterGridRowActivate(UltraGridRow row)  
    {  
        if (row != null)  
        {    
			for (int i = 0; i < keys.Length; i++)  
            { 
                keys[i] = row.Cells[keynames[i]].Value.ToString();  
            }  
            GoToRow(keys);  
        }  
    }  
	
	static bool ArraysEqual<T>(T[] a1, T[] a2)  
    {  
        if (ReferenceEquals(a1,a2)) { return true; }  
        if (a1 == null || a2 == null) { return false; }  
        if (a1.Length != a2.Length) { return false; }  
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;  
        for (int i = 0; i < a1.Length; i++)  
        {  
            if (!comparer.Equals(a1[i], a2[i])) { return false; }  
        }  
        return true;  
    }  

	protected virtual void OnRowChange(RowEventArgs args)
	{
		EventHandler<RowEventArgs> handler = RowChange;
		if (handler != null) { handler(this, args); }
	}

	protected virtual void OnGetData(GetDataEventArgs args)
	{
		EventHandler<GetDataEventArgs> handler = GetNewData;
		if (handler != null) { handler(this, args); }
	}

	protected virtual void OnFilteredChange(FilterEventArgs args)  
    {  
        EventHandler<FilterEventArgs> handler = FilteredChange;  
        if (handler != null) { handler(this, args); }  
    } 

	public event EventHandler<RowEventArgs> RowChange;

	public event EventHandler<GetDataEventArgs> GetNewData;

	public event EventHandler<FilterEventArgs> FilteredChange;  

}

class RowEventArgs : EventArgs
{
	private string _edvName;
	private int _rowIndex;
	private bool _rowChanged;	

	public string edvName
	{
		get { return _edvName; }
		set { _edvName = value; }
	}
	public int rowindex
	{
		get { return _rowIndex; }
		set { _rowIndex = value; }
	}
	public bool rowchanged
	{ 
		get { return _rowChanged; }
		set { _rowChanged = value; }
	}
}

class GetDataEventArgs : EventArgs
{
	private bool _gotData;

	public bool GotData
	{
		get { return _gotData; }
		set { _gotData = value; }
	}
}

class FilterEventArgs : EventArgs  
{  
}
