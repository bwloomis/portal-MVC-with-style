//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option or rebuild the Visual Studio project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Web.Application.StronglyTypedResourceProxyBuilder", "12.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Reminder {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Reminder() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Resources.Reminder", global::System.Reflection.Assembly.Load("App_GlobalResources"));
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TAKE THE TIME TO SET UP AN ELECTRONIC REMINDER OF YOUR NEXT RECERTIFICATION DATE.
        /// </summary>
        internal static string ReminderHeader {
            get {
                return ResourceManager.GetString("ReminderHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select this link to create reminders: Text or email reminder system that tells you when it is time to recertify, order your medications, or send in your bills! &lt;br/&gt; &lt;a href=&quot;http://www.safeplussound.org&quot; target=&quot;_blank&quot;&gt;&lt;img href=&quot;http://www.safeplussound.org&quot; src=&quot;/Content/adap/images/safe_plus_sound_logo.png&quot;/&gt; &lt;br/&gt; www.safeplussound.org&lt;/a&gt;.
        /// </summary>
        internal static string ReminderText {
            get {
                return ResourceManager.GetString("ReminderText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Submit Application and Return to Report List.
        /// </summary>
        internal static string SubmitAndReturn {
            get {
                return ResourceManager.GetString("SubmitAndReturn", resourceCulture);
            }
        }
    }
}
