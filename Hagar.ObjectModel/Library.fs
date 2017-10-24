namespace Hagar.ObjectModel

module ObjectModel =
    type VarInt = byte[]

    type SchemaType =
      | Expected
      | WellKnown of VarInt
      | Encoded of byte[]
      | Referenced of VarInt

    type DataField = {FieldIdDelta:uint32; SchemaType:SchemaType}

    type Field =
      | VarInt of DataField * byte[]


    let hello name =
        printfn "Hello %s" name

