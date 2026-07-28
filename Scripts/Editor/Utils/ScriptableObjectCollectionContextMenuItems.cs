using System;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionContextMenuItems
    {
        [MenuItem("CONTEXT/ScriptableObjectCollection/Create Generator", false, 99999)]
        private static void CreateGenerator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();

            GeneratorCreationWizard.Show(collectionType);
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Create Generator", true)]
        private static bool CreateGeneratorValidator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();
            return CollectionGenerators.GetGeneratorTypeForCollection(collectionType) == null;
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Edit Generator", false, 99999)]
        private static void EditGenerator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();
            Type generatorType = CollectionGenerators.GetGeneratorTypeForCollection(collectionType);

            if (ScriptUtility.TryGetScriptOfClass(generatorType, out MonoScript script))
                AssetDatabase.OpenAsset(script);
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Edit Generator", true)]
        private static bool EditGeneratorValidator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();
            return CollectionGenerators.GetGeneratorTypeForCollection(collectionType) != null;
        }
        
        
        [MenuItem("CONTEXT/ScriptableObjectCollection/Create Indirect Reference file", false, 99999)]
        private static void CreateIndirectReference(MenuCommand command)
        {
            ScriptableObjectCollection collection = (ScriptableObjectCollection)command.context;
            
            CodeGenerationUtility.GenerateIndirectAccessForCollectionItemType(collection.GetItemType());
        }
        
        [MenuItem("CONTEXT/ScriptableObjectCollection/Sort Items By Name", false, 99999)]
        private static void SortItemsByName(MenuCommand command)
        {
            ScriptableObjectCollection collection = (ScriptableObjectCollection)command.context;

            Undo.RecordObject(collection, "Sort Items By Name");
            collection.OrderByName();
            AssetDatabase.SaveAssetIfDirty(collection);

            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Sort Items By Name", true)]
        private static bool SortItemsByNameValidator(MenuCommand command)
        {
            ScriptableObjectCollection collection = (ScriptableObjectCollection)command.context;
            return !collection.ShouldProtectItemOrder && collection.Count > 1;
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Reset Settings", false, 1000)]
        private static void ResetSettings(MenuCommand command)
        {
            
            ScriptableObjectCollection collection = (ScriptableObjectCollection)command.context;

            SOCSettings.Instance.ResetSettings(collection);
        }
    }
}