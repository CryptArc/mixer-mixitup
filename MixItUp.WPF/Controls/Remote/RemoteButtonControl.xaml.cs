﻿using MixItUp.Base;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.Remote;
using MixItUp.WPF.Controls.MainControls;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace MixItUp.WPF.Controls.Remote
{
    /// <summary>
    /// Interaction logic for RemoteButtonControl.xaml
    /// </summary>
    public partial class RemoteButtonControl : UserControl
    {
        private RemoteControl remoteControl;
        private int xPosition;
        private int yPosition;

        private RemoteBoardItemModel item;

        public RemoteButtonControl()
        {
            InitializeComponent();
        }

        public void Initialize(RemoteControl remoteControl, int xPosition, int yPosition)
        {
            this.remoteControl = remoteControl;
            this.xPosition = xPosition;
            this.yPosition = yPosition;
        }

        public void SetRemoteItem(RemoteBoardItemModel item)
        {
            if (this.remoteControl.CurrentBoard != null && this.remoteControl.CurrentGroup != null && item != null && item.Command != null)
            {
                this.RemoveRemoteItem();
                this.item = item;

                this.item.SetValuesFromCommand();

                this.item.Size = RemoteBoardItemSizeEnum.OneByOne;
                this.item.XPosition = this.xPosition;
                this.item.YPosition = this.yPosition;

                this.NameTextBlock.Text = this.item.Name;
                this.DeleteButton.Visibility = System.Windows.Visibility.Visible;
                if (item is RemoteBoardButtonModel)
                {
                    RemoteBoardButtonModel button = this.item as RemoteBoardButtonModel;
                    this.NameTextBlock.Foreground = new BrushConverter().ConvertFromString(button.TextColor) as SolidColorBrush;
                    this.BackgroundColor.Fill = new BrushConverter().ConvertFromString(button.BackgroundColor) as SolidColorBrush;
                }

                if (!this.remoteControl.CurrentGroup.Items.Contains(this.item))
                {
                    this.remoteControl.CurrentGroup.Items.Add(this.item);
                }
                return;
            }
        }

        public void SetRemoteCommand(RemoteCommand command)
        {
            this.SetRemoteItem(new RemoteBoardButtonModel(command));
        }

        public void ClearCommand()
        {
            this.item = null;
            this.NameTextBlock.Text = "";
            this.DeleteButton.Visibility = System.Windows.Visibility.Collapsed;
            this.NameTextBlock.Foreground = Brushes.Black;
            this.BackgroundColor.Fill = Brushes.Transparent;
        }

        private void RemoveRemoteItem()
        {
            if (this.remoteControl.CurrentBoard != null && this.remoteControl.CurrentGroup != null)
            {
                this.remoteControl.CurrentGroup.Items.Remove(this.item);
            }
            this.ClearCommand();
        }

        private void ButtonRender_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.remoteControl.CurrentlySelectedCommand != null)
            {
                this.SetRemoteCommand(this.remoteControl.CurrentlySelectedCommand);
            }
            this.remoteControl.CurrentlySelectedCommand = null;
            this.remoteControl.ResetListSelectedIndex();

            e.Handled = true;
        }

        private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.remoteControl.CurrentGroup.Items.Remove(this.item);
            this.RemoveRemoteItem();
        }

        private void DeleteButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.DeleteButton.Opacity = 1.0;
        }

        private void DeleteButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.DeleteButton.Opacity = 0.3;
        }
    }
}
