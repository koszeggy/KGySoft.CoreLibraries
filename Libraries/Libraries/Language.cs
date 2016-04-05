#region Used namespaces

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

using Microsoft.Win32;

#endregion

namespace KGySoft.Libraries
{
    // TODO: Megnézni, nem egyszerűbb-e, ha [ThreadStatic] field-ekkel oldjuk  meg. Ekkor az add-remove és a local invoke is sokkal egyszerűbbé válna. A leiratkozás hiánya nek okoz több leaket mint a jelenlegi verzió.
    /// <summary>
    /// Represents the language settings of the current thread. Use this class if you want to be notified on
    /// language changes.
    /// </summary>
    public static class Language
    {
        #region Fields

        private static object syncRoot;        
        private static Dictionary<int, EventHandler> formattingLanguageChangedHandlers;
        private static Dictionary<int, EventHandler> displayLanguageChangedHandlers;
        private static bool captureSystemLocaleChange;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when formatting language (<see cref="Thread.CurrentCulture">Thread.CurrentThread.CurrentCulture</see>) has been changed by setting the
        /// <see cref="FormattingLanguage"/> property. Only the delegates subscribed from the affected thread are invoked.
        /// </summary>
        /// <remarks>
        /// Use this event if you want to be notified of changing the formatting culture of the current thread.
        /// Subscribers of this event are grouped by the thread of the subscription, so when this event is triggered,
        /// only the subscribers from the thread of the language change are notified.
        /// </remarks>
        /// <seealso cref="FormattingLanguage"/>
        /// <seealso cref="FormattingLanguageChangedGlobal"/>
        public static event EventHandler FormattingLanguageChanged
        {
            add
            {
                if (value == null)
                    return;

                lock (SyncRoot)
                {
                    int id = Thread.CurrentThread.ManagedThreadId;
                    EventHandler handler;
                    FormattingLanguageChangedHandlers.TryGetValue(id, out handler);
                    handler += value;
                    formattingLanguageChangedHandlers[id] = handler;
                }
            }

            remove
            {
                if (value == null || formattingLanguageChangedHandlers == null)
                    return;

                lock (SyncRoot)
                {
                    int id = Thread.CurrentThread.ManagedThreadId;
                    EventHandler handler;
                    if (!FormattingLanguageChangedHandlers.TryGetValue(id, out handler))
                        return;

                    handler -= value;
                    if (handler == null)
                        formattingLanguageChangedHandlers.Remove(id);
                    else
                        formattingLanguageChangedHandlers[id] = handler;
                }
            }
        }

        /// <summary>
        /// Occurs when formatting language (<see cref="Thread.CurrentCulture">Thread.CurrentThread.CurrentCulture</see>) has been changed in any <see cref="Thread"/>
        /// by setting <see cref="FormattingLanguage"/> property. The subscribed delegates are called from the affected thread,
        /// which can be different from the thread of the subscriptions.
        /// </summary>
        /// <remarks>
        /// The <see cref="FormattingLanguage"/> property reflects the formatting culture of the current thread (<see cref="Thread.CurrentCulture">Thread.CurrentThread.CurrentCulture</see>).
        /// This event triggers all subscribers regardless of their source thread in the current <see cref="AppDomain"/>.
        /// When the event triggered, the subscribers are invoked in the thread of the changed formatting language. You might
        /// want to check if the thread of the invokation is the same as the thread of the subscription.
        /// To notify subscribers from the affected thread only, use the <see cref="FormattingLanguageChanged"/> event instead.
        /// </remarks>
        /// <seealso cref="FormattingLanguage"/>
        /// <seealso cref="FormattingLanguageChanged"/>
        public static event EventHandler FormattingLanguageChangedGlobal;

        /// <summary>
        /// Occurs when display language (<see cref="Thread.CurrentUICulture">Thread.CurrentThread.CurrentUICulture</see>) has been changed by setting the
        /// <see cref="DisplayLanguage"/> property. Only the delegates subscribed from the affected thread are invoked.
        /// </summary>
        /// <remarks>
        /// Use this event if you want to be notified of changing the display culture of the current thread.
        /// Subscribers of this event are grouped by the thread of the subscription, so when this event is triggered,
        /// only the subscribers from the thread of the language change are notified.
        /// </remarks>
        /// <seealso cref="DisplayLanguage"/>
        /// <seealso cref="DisplayLanguageChangedGlobal"/>
        public static event EventHandler DisplayLanguageChanged
        {
            add
            {
                if (value == null)
                    return;

                lock (SyncRoot)
                {
                    int id = Thread.CurrentThread.ManagedThreadId;
                    EventHandler handler;
                    DisplayLanguageChangedHandlers.TryGetValue(id, out handler);
                    handler += value;
                    displayLanguageChangedHandlers[id] = handler;
                }
            }

            remove
            {
                if (value == null || displayLanguageChangedHandlers == null)
                    return;

                lock (SyncRoot)
                {
                    int id = Thread.CurrentThread.ManagedThreadId;
                    EventHandler handler;
                    if (!DisplayLanguageChangedHandlers.TryGetValue(id, out handler))
                        return;

                    handler -= value;
                    if (handler == null)
                        displayLanguageChangedHandlers.Remove(id);
                    else
                        displayLanguageChangedHandlers[id] = handler;
                }
            }
        }

        /// <summary>
        /// Occurs when display language (<see cref="Thread.CurrentUICulture">Thread.CurrentThread.CurrentUICulture</see>) has been changed in any <see cref="Thread"/>
        /// by setting <see cref="DisplayLanguage"/> property. The subscribed delegates are called from the affected thread,
        /// which can be different from the thread of the subscriptions.
        /// </summary>
        /// <remarks>
        /// The <see cref="DisplayLanguage"/> property reflects the display culture of the current thread (<see cref="Thread.CurrentUICulture">Thread.CurrentThread.CurrentUICulture</see>).
        /// This event triggers all subscribers regardless of their source thread in the current <see cref="AppDomain"/>.
        /// When the event triggered, the subscribers are invoked in the thread of the changed display language. You might
        /// want to check if the thread of the invokation is the same as the thread of the subscription.
        /// To notify subscribers from the affected thread only, use the <see cref="DisplayLanguageChanged"/> event instead.
        /// </remarks>
        /// <seealso cref="DisplayLanguage"/>
        /// <seealso cref="DisplayLanguageChanged"/>
        public static event EventHandler DisplayLanguageChangedGlobal;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the formatting language of the current <see cref="Thread"/> (<see cref="Thread.CurrentCulture">Thread.CurrentThread.CurrentCulture</see>).
        /// When set, <see cref="FormattingLanguageChanged"/> and <see cref="FormattingLanguageChangedGlobal"/> events are triggered.
        /// </summary>
        /// <remarks>
        /// <para>Formatting languagage represents the regional setting of formatting and parsing numbers, date and time values,
        /// currency, etc.</para>
        /// <para>Use this property instead of <see cref="Thread.CurrentCulture">Thread.CurrentThread.CurrentCulture</see> to
        /// keep language changes synchronized in your application.</para>
        /// <para>When this property is set, <see cref="FormattingLanguageChanged"/> and <see cref="FormattingLanguageChangedGlobal"/> events are triggered,
        /// which makes possible for example refreshing UI components displaying culture-specific formatted values.</para>
        /// </remarks>
        /// <value>The formatting language of the current <see cref="Thread"/>. By default equals to the language of formats
        /// of system regional settings.</value>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see kangword="null"/>.</exception>
        public static CultureInfo FormattingLanguage
        {
            get { return Thread.CurrentThread.CurrentCulture; }
            set
            {
                CultureInfo orig = Thread.CurrentThread.CurrentCulture;
                if (ReferenceEquals(orig, value))
                    return;

                Thread.CurrentThread.CurrentCulture = value;
                OnFormattingLanguageChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the display language of the current <see cref="Thread"/> (<see cref="Thread.CurrentUICulture">Thread.CurrentThread.CurrentUICulture</see>).
        /// When set, <see cref="DisplayLanguageChanged"/> and <see cref="DisplayLanguageChangedGlobal"/> events are triggered.
        /// </summary>
        /// <remarks>
        /// <para>Display languagage represents the language of the user interface of the application. This value is used when
        /// looking up localizable resources.</para>
        /// <para>Use this property instead of <see cref="Thread.CurrentUICulture">Thread.CurrentThread.CurrentUICulture</see> to
        /// keep language changes synchronized in your application.</para>
        /// <para>When this property is set, <see cref="DisplayLanguageChanged"/> and <see cref="DisplayLanguageChangedGlobal"/> events are triggered,
        /// which makes possible for example to refresh the language of the UI on the fly.</para>
        /// </remarks>
        /// <value>The display language of the current <see cref="Thread"/>. By default equals to the display of the operating system.</value>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see kangword="null"/>.</exception>
        public static CultureInfo DisplayLanguage
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
            set
            {
                CultureInfo orig = Thread.CurrentThread.CurrentUICulture;
                if (ReferenceEquals(orig, value))
                    return;

                Thread.CurrentThread.CurrentUICulture = value;
                OnDisplayLanguageChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets whether changes of system regional settings should be captured.
        /// When <c>true</c>, <see cref="FormattingLanguage"/> is updated on regional changes, and
        /// <see cref="FormattingLanguageChanged"/> and <see cref="FormattingLanguageChangedGlobal"/> events
        /// are triggered.
        /// </summary>
        /// <value>
        /// <c>true</c> if system regional settings should be captured; otherwise, <c>false</c>.
        /// Default value is <c>false</c>.
        /// </value>
        public static bool CaptureSystemLocaleChange
        {
            get { return captureSystemLocaleChange; }
            set
            {
                if (captureSystemLocaleChange == value)
                    return;

                captureSystemLocaleChange = value;
                if (value)
                    SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
                else
                    SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            }
        }

        #endregion

        #region Private Properties

        private static object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        private static Dictionary<int, EventHandler> FormattingLanguageChangedHandlers
        {
            get
            {
                return formattingLanguageChangedHandlers
                    ?? (formattingLanguageChangedHandlers = new Dictionary<int, EventHandler>());
            }
        }

        private static Dictionary<int, EventHandler> DisplayLanguageChangedHandlers
        {
            get
            {
                return displayLanguageChangedHandlers
                    ?? (displayLanguageChangedHandlers = new Dictionary<int, EventHandler>());
            }
        }

        #endregion

        #endregion

        #region Methods

        private static void OnFormattingLanguageChanged(EventArgs e)
        {
            // raising the global event
            EventHandler globalHandler = FormattingLanguageChangedGlobal;
            if (globalHandler != null)
            {
                globalHandler.Invoke(null, e);
            }

            // raising the local event
            lock (SyncRoot)
            {
                if (formattingLanguageChangedHandlers == null)
                    return;

                EventHandler handler;
                if (formattingLanguageChangedHandlers.TryGetValue(Thread.CurrentThread.ManagedThreadId, out handler))
                    handler.Invoke(null, e);
            }
        }

        private static void OnDisplayLanguageChanged(EventArgs e)
        {
            // raising the global event
            EventHandler globalHandler = DisplayLanguageChangedGlobal;
            if (globalHandler != null)
            {
                globalHandler.Invoke(null, e);
            }

            // raising the local event
            lock (SyncRoot)
            {
                if (displayLanguageChangedHandlers == null)
                    return;

                EventHandler handler;
                if (displayLanguageChangedHandlers.TryGetValue(Thread.CurrentThread.ManagedThreadId, out handler))
                    handler.Invoke(null, e);
            }
        }

        #endregion

        #region Event Handlers
        //ReSharper disable InconsistentNaming

        static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.Locale)
                return;

            Thread.CurrentThread.CurrentCulture.ClearCachedData();
            OnFormattingLanguageChanged(EventArgs.Empty);
        }

        //ReSharper restore InconsistentNaming
        #endregion
    }
}
