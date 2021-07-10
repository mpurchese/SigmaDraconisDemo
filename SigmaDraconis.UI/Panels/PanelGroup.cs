namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;

    /// <summary>
    /// Class for a group of panels
    /// </summary>
    internal class PanelGroup
    {
        private List<PanelBase> panels = new List<PanelBase>();

        internal List<PanelBase> CurrentPanels { get; private set; } = new List<PanelBase>();

        internal PanelBase Add(IUIElement parent, PanelBase panel)
        {
            this.panels.Add(panel);
            parent.AddChild(panel);
            return panel;
        }

        internal void Update()
        {
            foreach (var panel in this.CurrentPanels)
            {
                if (panel.IsVisible == false && this.panels.All(s => !s.IsVisible || this.CurrentPanels.Contains(s)))
                {
                    panel.Show();
                }
            }
        }

        internal void Hide(PanelBase panel)
        {
            if (panel.IsShown) panel.Hide();
            if (this.CurrentPanels.Contains(panel)) this.CurrentPanels.Remove(panel);
        }

        internal void Show(PanelBase panel, bool hideExisting = true)
        {
            if (hideExisting)
            {
                this.CurrentPanels.Clear();
                if (panel.IsVisible)
                {
                    panel.Hide();
                }
                else
                {
                    this.CurrentPanels.Add(panel);
                    foreach (var s in this.panels)
                    {
                        if (s != panel && s.IsVisible)
                        {
                            s.Hide();
                        }
                    }
                }
            }
            else if (!this.CurrentPanels.Contains(panel))
            {
                this.CurrentPanels.Add(panel);
            }
        }

        internal void Show(List<PanelBase> panelsToShow)
        {
            this.CurrentPanels.Clear();
            foreach (var s in this.panels)
            {
                if (panelsToShow.Contains(s))
                {
                    this.CurrentPanels.Add(s);
                    //if (!s.IsVisible) s.Show();
                }
                else if (s.IsVisible)
                {
                    s.Hide();
                }
            }
        }

        internal void HideAll()
        {
            foreach (var panel in this.panels.Where(s => s.IsVisible))
            {
                panel.Hide();
            }

            this.CurrentPanels.Clear();
        }
    }
}
