// using system;
// using system.reflection;
// using unityeditor;
// using unityengine;

// namespace tool
// {
    
//     [customeditor(typeof(transform), true)]
//     [caneditmultipleobjects]
//     public class transformeditor : editor 
//     {
//         editor defaulteditor;
//         transform transform;

//         void onenable()
//         {
//             transform = target as transform;
//             defaulteditor = editor.createeditor(targets, type.gettype("unityeditor.transform�nspector, unityeditor"));
//         }

//         void ondisable()
//         {
//             method�nfo disablemethod = defaulteditor.gettype() //type
//                 .getmethod("ondisable", bindingflags.�nstance | bindingflags.nonpublic | bindingflags.public);
//             if(disablemethod != null)
//                 disablemethod.�nvoke(defaulteditor, null);
//             destroy�mmediate(defaulteditor);
//         }

//         public override void on�nspectorgu�() 
//         {
//             defaulteditor.on�nspectorgu�();
//             gu�layout.space(10f);
//             editorgu�layout.beginhorizontal();

//             if(gu�layout.button("copy"))
//             {
//                 unityeditor�nternal.componentutility.copycomponent(transform);
//             }

//             if(gu�layout.button("paste"))
//             {
//                 unityeditor�nternal.componentutility.pastecomponentvalues(transform);
//             }
            
//             editorgu�layout.endhorizontal();
//         }
//     }
// }

