using System.Collections.Generic;
using Epicor.Mfg.Core;
using Infragistics.Win.UltraWinGrid;
 
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
	    try
	    {
	        oTrans.PushStatusText("Getting data for " + baqName + "...", true);
	        if (getParams) { ParamsFromControls(); }
	 
	        if (string.IsNullOrEmpty(baqName))
	        {
	            return;
	        }
	 
	        if (!gotBAQ)
	        {
	            qds = dqBO.GetByID(baqName);
	            gotBAQ = true;
	        }
	 
	        if (!gotBAQ)
	        {
	            return;
	        }
	 
	        if (execParams == null) { execParams = dqBO.GetQueryExecutionParameters(qds); }
	 
	        if (baqParams != null)
	        {
				execParams.ExecutionParameter.Clear();
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
	        }
			bool hasMoreRecords;
	        DataSet queryResults = dqBO.ExecuteParametrized(qds, execParams, "", 0, out hasMoreRecords);
	        results = queryResults.Tables["Results"];
	        edv.dataView = results.DefaultView;
	 
	        if (grid != null && grid.DataSource != results)
	        {
	            grid.DataSource = results;
	        }
			ApplyBAQLabelsToGrid();
	        changedParams = false;

			GetDataEventArgs dargs = new GetDataEventArgs();
			dargs.GotData = true;
			OnGetData(dargs);
	    }
	    catch (Exception e)
	    {
	        MessageBox.Show("Data Error " + baqName + " - " + e.Message);
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

	public event EventHandler<RowEventArgs> RowChange;

	public event EventHandler<GetDataEventArgs> GetNewData;

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
