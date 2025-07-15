// download from https://github.com/giuliano-marinelli/UnityFoldoutDecorator

using UnityEngine;

namespace NiloToon.NiloToonURP
{
    public class FoldoutAttribute : PropertyAttribute
    {
        public string name;
        public bool foldEverything;
        public bool readOnly;
        public bool styled;

        /// <summary>Adds the property to the specified foldout group.</summary>
        /// <param name="name">Name of the foldout group.</param>
        /// <param name="foldEverything">Toggle to put all properties to the specified group</param>
        /// <param name="readOnly">Toggle to put all properties to the specified group</param>
        public FoldoutAttribute(string name, bool foldEverything = true, bool readOnly = false, bool styled = true)
        {
            this.foldEverything = foldEverything;
            this.name = name;
            this.readOnly = readOnly;
            this.styled = styled;
        }
    }
}