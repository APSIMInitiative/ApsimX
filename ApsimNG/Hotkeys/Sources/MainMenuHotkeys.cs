using System;
using System.Collections.Generic;
using System.Reflection;
using Models.Core;
using UserInterface.Presenters;

namespace UserInterface.Hotkeys
{
    /// <summary>
    /// A class to fetch hotkey metadata from the main menu.
    /// </summary>
    public class MainMenuHotkeys : IHotkeySource
    {
        /// <summary>
        /// Fetch hotkey metadata from the main menu.
        /// </summary>
        public IEnumerable<IHotkey> GetHotkeys()
        {
            Type menuType = typeof(MainMenu);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (MethodInfo method in menuType.GetMethods(flags))
            {
                MainMenuAttribute attribute = method.GetCustomAttribute<MainMenuAttribute>();
                if (attribute == null)
                    continue;
                yield return new Hotkey(attribute.Hotkey, attribute.MenuName);
            }
        }
    }
}
