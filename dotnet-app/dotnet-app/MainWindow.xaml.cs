﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tick42;
using Tick42.Channels;
using Tick42.Contexts;
using Tick42.StickyWindows;
using Tick42.Windows;

namespace dotnet_app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IGlueChannelEventHandler
    {
        private Glue42 glue;
        private IGlueWindow glueWindow;
        private IContext instrumentSelectionContext;
        private IContext portfolioContext;

        /// <summary>
        /// In order for this field path to work, we have two changes in %localappdata%\tick42\GlueDesktop\config\channels.json
        /// 1. Change *all* readDataFieldPath and writeDataFieldPath to data.instrument.bbg
        /// 2. Change *all* read and write fields to "blp" -- to force bridge to read/write Bloomberg instrument codes and not RIC
        /// </summary>

        private string instrumentFieldPath_ = "data.instrument.bbg";
        private const string PortfolioContextName = "Portfolio";
        private const string PortfolioIdKey = "PortfolioId";
        private const string InstrumentSelectionContextName = "InstrumentSelection";
        private const string SelectedInstrumentKey = "SelectedInstrument";

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the portfolios
            var portfolios = new string[] { "High-risk", "Moderate", "Low-risk" };
            lbPortfolios.ItemsSource = portfolios;

            this.glue = new Glue42();
            this.glue.Initialize("DotNetApp", useContexts: true);

            // Initialize Window Stickiness and read from config:
            var swOptions = this.glue.GlueWindows?.GetStartupOptions() ?? new GlueWindowOptions();
            swOptions.WithChannelSupport(true);
            this.glue.GlueWindows?.RegisterWindow(this, swOptions)
                .ContinueWith(r =>
                {
                    glueWindow = r.Result;
                    glueWindow.Title = "Glue42 Bloomberg Net App";
                    glueWindow.ChannelContext.Subscribe(new LambdaGlueChannelEventHandler<string>(
                        (context, channel, instrument) =>
                            Dispatcher.BeginInvoke((Action) (() => tbBBG.Text = instrument)),
                        (context, channel, arg3) => Dispatcher.BeginInvoke((Action) (() => tbBBG.Text =
                            context.GetValue<string>(
                                instrumentFieldPath_,
                                ChannelLevel.Root)))), instrumentFieldPath_, ChannelLevel.Root);
                });

        }

        private void LbPortfolios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.portfolioContext.Update(ctx => {
                ctx[PortfolioIdKey] = lbPortfolios.SelectedItem;
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.portfolioContext = await this.glue.Contexts.GetContext(PortfolioContextName);
            this.instrumentSelectionContext = await this.glue.Contexts.GetContext(InstrumentSelectionContextName);
            this.instrumentSelectionContext.ContextUpdated += InstrumentSelectionContext_ContextUpdated;
        }

        private void InstrumentSelectionContext_ContextUpdated(object sender, ContextUpdatedEventArgs e)
        {
            var context = sender as IContext;
            if (context == null)
            {
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                lblSelectedInstrumentName.Content = ((string)context[SelectedInstrumentKey]) ?? "None";
            }));
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.glueWindow.ChannelContext.SetValue(tbBBG.Text, instrumentFieldPath_, ChannelLevel.Root);
        }

        public void HandleChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel, IGlueChannel prevChannel)
        {
        }

        public void HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel, ContextUpdatedEventArgs updateArgs)
        {
        }
    }
}
