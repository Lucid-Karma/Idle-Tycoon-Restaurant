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
//             defaulteditor = editor.createeditor(targets, type.gettype("unityeditor.transformýnspector, unityeditor"));
//         }

//         void ondisable()
//         {
//             methodýnfo disablemethod = defaulteditor.gettype() //type
//                 .getmethod("ondisable", bindingflags.ýnstance | bindingflags.nonpublic | bindingflags.public);
//             if(disablemethod != null)
//                 disablemethod.ýnvoke(defaulteditor, null);
//             destroyýmmediate(defaulteditor);
//         }

//         public override void onýnspectorguý() 
//         {
//             defaulteditor.onýnspectorguý();
//             guýlayout.space(10f);
//             editorguýlayout.beginhorizontal();

//             if(guýlayout.button("copy"))
//             {
//                 unityeditorýnternal.componentutility.copycomponent(transform);
//             }

//             if(guýlayout.button("paste"))
//             {
//                 unityeditorýnternal.componentutility.pastecomponentvalues(transform);
//             }
            
//             editorguýlayout.endhorizontal();
//         }
//     }
// }

