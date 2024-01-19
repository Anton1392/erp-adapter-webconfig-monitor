﻿namespace AdapterLibrary;

using System.Numerics;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Runtime.Versioning;

public record VariableValue( 
  int Type, // String: 0, Numeric: 1, Boolean: 2, Date: 3
  string? StringValue,
  bool? BooleanValue,
  double? NumericValue,
  DateTime? DateTimeValue
);

public record VariableState(
  string Id,
  string Name,
  int VariableType, // String: 0, Numeric: 1, Boolean: 2, Date: 3
  VariableValue Value
);

public record VariableUpdate(
  string VariableId,
  VariableValue Value
);

public record UpdatePartConfigurationInstruction(
  int Type, // 0 = Variable, 1 = SelectionGroupRow
  VariableUpdate? Variable,
  SelectionGroupRowUpdate? SelectionGroupRow
);

public record SelectionGroupRowState(
  string Id,
  string PartId,
  bool IsSelected,
  int Quantity
);

public record SelectionGroupRowUpdate(
  string SelectionGroupRowId,
  bool? Selected,
  int? Quantity
);

public record SelectionGroupState(
  string Code,
  SelectionGroupRowState[] Rows
);

public record SectionState (
  string Id,
  VariableState[] Variables,
  SelectionGroupState[] SelectionGroups

);

public record PartConfigurationState(
  string SessionId,
  bool IsValid,
  string PartId,
  int Quantity,
  SectionState[] Sections
  // "ExpiresAt": "0001-01-01T00:00:00+00:00",
  // "CustomerId": 0,
  // "Comment": null,
  // "PartConfigurationTemplateSnapshotId": 0,
  // "PartConfigurationTemplateId": 0,
  // "PartConfigurationTemplateVersion": 0,
  // "PartConfigurationId": 0,
  // "AlternativePreparationCode": null,
  // "DiscountPercentage": 0.0,
  // "LockedDiscount": false,
  // "LockedUnitPrice": false,
  // "PriceFormulaFactor": 0.0,
  
  // "StandardPrice": null,
  // "UnitPrice": null,
  // "UnitPriceInCompanyCurrency": null,
  // "WeightPerUnit": null,
  
);

public record PartNumberMap (
  string Id,
  string PartNumber
);



public class WebValidationResult
 {

  public required string Id;

  public required string Description;

  public required string[] ErrorMessages;

}

public record WebSelectionRowItem (

  // string id,
  // string partId,
  string selection,
  // string partDescription,
  // string partNumber,
  int quantity



  // public required WebValidationResult[] validationResults;

);

public record WebSelectionGroupState(

  string code,
  string description

  // WebSelectionRowItem[] values
);

public record WebVariableState(
  string name,
  // public required string description;

  double value

  // public required WebValidationResult[] validationResults;
);

public record WebConfigurationState(

    string partNumber,
    Dictionary<string, double>[] values,
    Dictionary<string, string>[] texts,
    Dictionary<string, WebSelectionRowItem>[] selections
    // public required string partConfigurationId;
    // public required string configurationSessionId;
    // public required int quantity;

    // public required Dictionary<string, WebVariableState>[] Variables;
    // public required Dictionary<string, WebSelectionGroupState>[] SelectionGroups;
);

public class MonitorAPI
{

  string getPartNumberFromId (string id, List<PartNumberMap> partNumberList) {
    var partNumber = partNumberList.Find(item => item.Id == id);
    if (partNumber != null) {
      return partNumber.PartNumber;
    } else {
      throw new Exception();
    }
  }

  public string configurationToWeb (string partConfigurationStateJSON, string partNumberListJSON) {

    PartConfigurationState? partConfigurationState = JsonSerializer.Deserialize<PartConfigurationState>(partConfigurationStateJSON, 
      new JsonSerializerOptions(JsonSerializerDefaults.General)
    );

    List<PartNumberMap>? partIdList = JsonSerializer.Deserialize<List<PartNumberMap>>(partNumberListJSON, new JsonSerializerOptions(JsonSerializerDefaults.General) ); // todo: Replace when $expand is suppoted for PartNumber on PartConfigurationState (issue submitted to support)

    
    var values = new Dictionary<string, double>();
    var texts = new Dictionary<string, string>();
    var selections = new Dictionary<string, List<WebSelectionRowItem>>();
        // {
        //   { "Width", new WebVariableState("707434600696463128", "Width", 100) },
        // };
    if (partConfigurationState != null) {
      
      for (int i = 0; i < partConfigurationState.Sections.Length; i++) 
      {

        var section = partConfigurationState.Sections[i];
        if (section != null) {
          for (int j = 0; j < section.Variables.Length; j++) {
            var secVariable = section.Variables[j];
            if (secVariable != null) {
              if ((secVariable.VariableType == 1) & (secVariable.Value.Type == 1) & (secVariable.Value.NumericValue != null)) {
                double value = secVariable.Value.NumericValue ?? 0;
                values.Add(secVariable.Name, value);
              } else {
                // todo: Add String to texts and Date to dates
              }
            }
          }

          for (int j = 0; j < section.SelectionGroups.Length; j++) {
            var selGroup = section.SelectionGroups[j];
            if (selGroup != null) {
              List<WebSelectionRowItem> selectedRows = new List<WebSelectionRowItem>();
              
              for (int k = 0; k < selGroup.Rows.Length; k++) {
                var row = selGroup.Rows[k];
                if (row.IsSelected) {
                  var rowPartNumber = getPartNumberFromId(row.PartId, partIdList ?? ([]));
                  var webValue = new WebSelectionRowItem(rowPartNumber, row.Quantity);
                  selectedRows.Add(webValue);
                }
              }

              selections.Add(selGroup.Code, selectedRows);
                // var value = (sectionVariable.Value.NumericValue != null) ? sectionVariable.Value.NumericValue : 0;
              
            }
          }
        }
      }
    }

    var partId = (partConfigurationState != null) ? partConfigurationState.PartId : "";
    var partNumber = getPartNumberFromId(partId, partIdList ?? ([]));

    bool valid = (partConfigurationState != null) ? partConfigurationState.IsValid : false;

    var state = new 
    {
        partNumber,
        valid,
        // partConfigurationId = "",
        // configurationSessionId = "",
        // quantity = 1,
        values,
        texts,
        selections,
        // Variables = [],
    };

    string json = JsonSerializer.Serialize(state);

    return json;
  }

  public string webToConfigurationInstructions (string webConfigStateJSON, string sessionId, string partNumberListJSON) {

    var instructions = new 
      {
        SessionId = "b6ee6341-92ed-483a-85f5-73180bc04c42",
        Instructions = new List<UpdatePartConfigurationInstruction>(){
          new UpdatePartConfigurationInstruction(
            0, 
            new VariableUpdate("707434600696463128", 
              new VariableValue(1, null, null, 100, null)
            ),
            null
          ),
          new UpdatePartConfigurationInstruction(
            0, 
            new VariableUpdate("707434668342198034", 
              new VariableValue(1, null, null, 100, null)
            ),
            null
          ),
           new UpdatePartConfigurationInstruction(
            1, 
            null,
            new SelectionGroupRowUpdate("993518285080342908", true, null)
          ),
        }
        
      };

      string json = JsonSerializer.Serialize(instructions);

    return json;
  }
}
