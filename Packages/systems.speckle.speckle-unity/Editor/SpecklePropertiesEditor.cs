using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;
using UnityEditor;
using UnityEngine;
using Mesh = Objects.Geometry.Mesh;
using Object = UnityEngine.Object;

#nullable enable
namespace Speckle.ConnectorUnity.Editor
{
    [CustomEditor(typeof(SpeckleProperties))]
    public class SpecklePropertiesEditor : UnityEditor.Editor
    {
        private static readonly string[] SpeckleTypeOptionStrings;
        private static readonly Type[] SpeckleTypeOptions;

        private HashSet<string> ArrayFoldoutState = new HashSet<string>();
        
        static SpecklePropertiesEditor()
        {
            var options = typeof(Mesh).Assembly
                .GetTypes()
                .Where(x => x.IsSubclassOf(typeof(Base)) && !x.IsAbstract).ToList();

            var strings = options
                .Where(x => x.FullName != null)
                .Select(x => x.FullName!.Replace('.', '/'));
            

            SpeckleTypeOptionStrings = strings.Append(nameof(Base)).ToArray();
            SpeckleTypeOptions = options.Append(typeof(Base)).ToArray();
            Debug.Assert(SpeckleTypeOptions.Length == SpeckleTypeOptionStrings.Length);
        }
        
        private static GUILayoutOption[] propLayoutOptions = { GUILayout.ExpandWidth(true) };
        
        public override void OnInspectorGUI()
        {
            SpeckleProperties properties = (SpeckleProperties) target;

            // SpeckleType
            GUILayout.Label("Speckle Type: ");
            
            var oldIndex = Array.IndexOf(SpeckleTypeOptions, properties.SpeckleType);
            var speckleTypeSelectedIndex = EditorGUILayout.Popup(oldIndex, SpeckleTypeOptionStrings);
            
            if(oldIndex != speckleTypeSelectedIndex && speckleTypeSelectedIndex >= 0)
            {
                properties.SpeckleType = SpeckleTypeOptions[speckleTypeSelectedIndex];
            }
            
            //Properties
            GUILayout.Label("Properties: ");
            
            foreach (var kvp in properties.Data)
            {
                var existingValue = kvp.Value;
                var newValue = CreateField(existingValue, kvp.Key, propLayoutOptions);
                if(newValue != existingValue)
                    properties.Data[kvp.Key] = newValue;
                
                GUILayout.Space(10);
            }
        }

        private object? CreateField(object? v, string propName, params GUILayoutOption[] options)
        {
            object? ret = v switch
            {
                Enum e => EditorGUILayout.EnumPopup(propName, e, options),
                Object o => EditorGUILayout.ObjectField(propName, o, o.GetType(), true, options),
                IList l => ArrayField(propName, l, options),
                _ => CreateFieldPrimitive(v, propName, options),
            };
            if (ret != null) return ret;

            EditorGUILayout.TextField(propName, v == null? "NULL" : v.ToString());
            return v;
        }
        
        private static object? CreateFieldPrimitive(object? v, string propName, params GUILayoutOption[] options)
        {
            return v switch
            {
                int i => EditorGUILayout.IntField(propName, i, options),
                long l => EditorGUILayout.LongField(propName, l, options),
                float f => EditorGUILayout.FloatField(propName, f, options),
                double d => EditorGUILayout.DoubleField(propName, d, options),
                string s => EditorGUILayout.TextField(propName, s, options),
                bool b => GUILayout.Toggle(b, propName, options),
                Enum e => EditorGUILayout.EnumPopup(propName, e, options),
                Point p => PointToVector3(EditorGUILayout.Vector3Field(propName, new Vector3((float)p.x, (float)p.z, (float)p.z), options), p),
                _ => null,
            };
        }
        
        private static Point PointToVector3(Vector3 vector, Point p)
        {
            p.x = vector.x;
            p.y = vector.y;
            p.z = vector.z;
            return p;
        }
        
        private IList ArrayField(string propName, IList list, params GUILayoutOption[] options)
        {
            bool isExpanded = EditorGUILayout.Foldout(ArrayFoldoutState.Contains(propName), propName);
            if (isExpanded)
            {
                ArrayFoldoutState.Add(propName);
                for (int i = 0; i < list.Count; i++)
                {
                    object? item = list[i];
                    var r = CreateFieldPrimitive(item, i.ToString(), options);
                    
                    if (r == null)
                    {
                        EditorGUILayout.TextField(i.ToString(), item == null? "NULL" : item.ToString());
                        continue;
                    }
                    //Update list item
                    list[i] = r;
                }
            }
            else
            {
                ArrayFoldoutState.Remove(propName);
            }

            return list;
        }
    }
}
