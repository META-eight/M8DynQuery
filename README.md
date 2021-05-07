# M8DynQuery
Epicor Client Class for Dynamic Query

One variable per BAQ needed, at the Script level (after “// Add Custom Module Level Variables Here **”):

	private M8DynQuery dqXXXX;

Declare and initialise those variables within InitializeCustomCode:

	dqXXXX = new M8DynQuery(
			"BAQNAME_HERE", // string, name of BAQ
			oTrans, // always oTrans
			csm.GetNativeControlReference("guid"), // use GUID of the panel where the controls are
			grdXXXX, // grid for the data, or null if none
			new string[] {"PARAM1","PARAM2"}, // string array of BAQ parameter names, or null if none
			new string[] {"-1",string.Empty} // string array of initial parameter values, or null if no parameters
		);

Dispose of the object in DestroyCustomCode, after "// Begin Custom Code Disposal":

	dqXXXX.Dispose();

### Methods:

GetData() - redownloads the BAQ data.

RefreshData() - redownloads the BAQ data only if the parameters have been changed.

Save() - saves the updated BAQ data, assuming the BAQ is updateable.

UpdateParam(string paramName, string paramValue) - updates the named parameter.

ResetParams() - sets all parameters back to the initial defaults.

Clear() - resets all data and the Dynamic Query.


### Properties:

Adapter() - returns the DynamicQueryAdapter for the M8DynQuery object.

EpiDataView() - returns the associated EpiDataView (which is also in the oTrans.EpiDataViews collection).

DataTable - returns the underlying DataTable (DynamicQuery results).

CurrentDataRow() - the DataRowView of the active EpiDataView row, or null if none is active.

ParamNames() - a List<string> of the parameter names.
  
Params() - a Dictionary<string,string> of parameter names and current values.

### Features:

There are two conveniences built in.

Automatic drop-downs within the grid - if drop-down controls are used within the screen and bound to fields in the EpiDataView used by the M8DynQuery object, those drop-downs will be pushed to the same fields within the grid so both behave in the same way.

Parameter input boxes - if a control is placed within the screen, unbound, and named according to the M8DynQuery convention, it will automatically be treated as the input field for the associated BAQ parameter. The naming convention for this is "param" + the BAQ name + the parameter name.
