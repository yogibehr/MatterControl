﻿/*
Copyright (c) 2018, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using System;
using System.Collections.Generic;

namespace MatterHackers.MatterControl
{
	public class WizardControlPage : GuiWidget
	{
		private string stepDescription = "";

		public string StepDescription { get { return stepDescription; } set { stepDescription = value; } }

		public WizardControlPage(string stepDescription)
		{
			StepDescription = stepDescription;
		}

		public virtual void PageIsBecomingActive()
		{
		}

		public virtual void PageIsBecomingInactive()
		{
		}
	}

	public abstract class WizardControl : GuiWidget
	{
		double extraTextScaling = 1;

		private FlowLayoutWidget pageContent;
		public Button nextButton;
		protected Button doneButton;
		protected Button cancelButton;

		private TextWidget stepDescriptionWidget;

		protected abstract IEnumerator<WizardControlPage> Pages { get; }

		public string StepDescription
		{
			get { return stepDescriptionWidget.Text; }
			set { stepDescriptionWidget.Text = value; }
		}

		public WizardControl()
		{
			var buttonFactory = ApplicationController.Instance.Theme.ButtonFactory;

			FlowLayoutWidget topToBottom = new FlowLayoutWidget(FlowDirection.TopToBottom);
			topToBottom.AnchorAll();
			topToBottom.Padding = new BorderDouble(3, 0, 3, 5);

			FlowLayoutWidget headerRow = new FlowLayoutWidget(FlowDirection.LeftToRight);
			headerRow.HAnchor = HAnchor.Stretch;
			headerRow.Margin = new BorderDouble(0, 3, 0, 0);
			headerRow.Padding = new BorderDouble(0, 3, 0, 3);

			{
				stepDescriptionWidget = new TextWidget("", pointSize: 14 * extraTextScaling);
				stepDescriptionWidget.AutoExpandBoundsToText = true;
				stepDescriptionWidget.TextColor = ActiveTheme.Instance.PrimaryTextColor;
				stepDescriptionWidget.HAnchor = HAnchor.Stretch;
				stepDescriptionWidget.VAnchor = Agg.UI.VAnchor.Bottom;

				headerRow.AddChild(stepDescriptionWidget);
			}

			topToBottom.AddChild(headerRow);

			AnchorAll();
			BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;

			pageContent = new FlowLayoutWidget();
			pageContent.BackgroundColor = ActiveTheme.Instance.SecondaryBackgroundColor;
			pageContent.Padding = new BorderDouble(3);

			topToBottom.AddChild(pageContent);
			topToBottom.Margin = new BorderDouble(bottom: 3);

			{
				FlowLayoutWidget buttonBar = new FlowLayoutWidget();
				buttonBar.HAnchor = Agg.UI.HAnchor.Stretch;
				buttonBar.Padding = new BorderDouble(0, 3);

				nextButton = buttonFactory.Generate("Next".Localize());
				nextButton.Name = "Next Button";
				nextButton.Click += next_Click;

				doneButton = buttonFactory.Generate("Done".Localize());
				doneButton.Name = "Done Button";
				doneButton.Click += done_Click;

				cancelButton = buttonFactory.Generate("Cancel".Localize());
				cancelButton.Click += done_Click;
				cancelButton.Name = "Cancel Button";

				buttonBar.AddChild(nextButton);
				buttonBar.AddChild(new HorizontalSpacer());
				buttonBar.AddChild(doneButton);
				buttonBar.AddChild(cancelButton);

				topToBottom.AddChild(buttonBar);
			}

			pageContent.AnchorAll();

			AddChild(topToBottom);
		}

		IEnumerator<WizardControlPage> pagesCache;
		public override void Initialize()
		{
			if(pagesCache == null)
			{
				pagesCache = Pages;
			}

			next_Click(this, null);

			base.Initialize();
		}

		private void done_Click(object sender, EventArgs mouseEvent)
		{
			GuiWidget windowToClose = this;
			while (windowToClose != null && windowToClose as SystemWindow == null)
			{
				windowToClose = windowToClose.Parent;
			}

			SystemWindow topSystemWindow = windowToClose as SystemWindow;
			if (topSystemWindow != null)
			{
				topSystemWindow.CloseOnIdle();
			}
		}

		private void next_Click(object sender, EventArgs mouseEvent)
		{
			pagesCache.Current?.PageIsBecomingInactive();

			pageContent.CloseAllChildren();
			pagesCache.MoveNext();

			StepDescription = pagesCache.Current.StepDescription;
			pageContent.AddChild(pagesCache.Current);
			pagesCache.Current?.PageIsBecomingActive();
		}
	}
}