﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.DesignTools;
using MatterHackers.MatterControl.DesignTools.Operations;
using MatterHackers.PolygonMesh;

namespace MatterHackers.MatterControl.PartPreviewWindow.View3D
{
	public class CombineObject3D_2 : OperationSourceContainerObject3D, IPropertyGridModifier
	{
		public CombineObject3D_2()
		{
			Name = "Combine";
		}

		public BooleanProcessing.ProcessingModes Processing { get; set; } = BooleanProcessing.ProcessingModes.Polygons;

		[EnumDisplay(Mode = EnumDisplayAttribute.PresentationMode.Buttons)]
		public BooleanProcessing.ProcessingResolution OutputResolution { get; set; } = BooleanProcessing.ProcessingResolution._64;

		[EnumDisplay(Mode = EnumDisplayAttribute.PresentationMode.Buttons)]
		public BooleanProcessing.IplicitSurfaceMethod MeshAnalysis { get; set; }

		[EnumDisplay(Mode = EnumDisplayAttribute.PresentationMode.Buttons)]
		public BooleanProcessing.ProcessingResolution InputResolution { get; set; } = BooleanProcessing.ProcessingResolution._64;

		public override Task Rebuild()
		{
			this.DebugDepth("Rebuild");

			var rebuildLocks = this.RebuilLockAll();

			return ApplicationController.Instance.Tasks.Execute(
				"Combine".Localize(),
				null,
				(reporter, cancellationToken) =>
				{
					var progressStatus = new ProgressStatus();
					reporter.Report(progressStatus);

					try
					{
						Combine(cancellationToken, reporter);
					}
					catch
					{
					}

					UiThread.RunOnIdle(() =>
					{
						rebuildLocks.Dispose();
						Parent?.Invalidate(new InvalidateArgs(this, InvalidateType.Children));
					});
					return Task.CompletedTask;
				});
		}

		public void Combine()
		{
			Combine(CancellationToken.None, null);
		}

		private void Combine(CancellationToken cancellationToken, IProgress<ProgressStatus> reporter)
		{
			SourceContainer.Visible = true;
			RemoveAllButSource();

			var participants = SourceContainer.VisibleMeshes();
			if (participants.Count() < 2)
			{
				if (participants.Count() == 1)
				{
					var newMesh = new Object3D();
					newMesh.CopyProperties(participants.First(), Object3DPropertyFlags.All);
					newMesh.Mesh = participants.First().Mesh;
					this.Children.Add(newMesh);
					SourceContainer.Visible = false;
				}

				return;
			}

			var items = participants.Select(i => (i.Mesh, i.WorldMatrix(SourceContainer)));
			var resultsMesh = BooleanProcessing.DoArray(items,
				BooleanProcessing.CsgModes.Union,
				Processing,
				InputResolution,
				OutputResolution,
				reporter,
				cancellationToken);

			if (resultsMesh != null)
			{
				var resultsItem = new Object3D()
				{
					Mesh = resultsMesh
				};
				resultsItem.CopyProperties(participants.First(), Object3DPropertyFlags.All & (~Object3DPropertyFlags.Matrix));
				this.Children.Add(resultsItem);
				SourceContainer.Visible = false;
			}
		}

		private bool ProcessPolygons
        {
			get => Processing == BooleanProcessing.ProcessingModes.Polygons || Processing == BooleanProcessing.ProcessingModes.libigl;
        }

		public void UpdateControls(PublicPropertyChange change)
		{
			change.SetRowVisible(nameof(InputResolution), () => !ProcessPolygons);
			change.SetRowVisible(nameof(OutputResolution), () => !ProcessPolygons);
			change.SetRowVisible(nameof(MeshAnalysis), () => !ProcessPolygons);
			change.SetRowVisible(nameof(InputResolution), () => !ProcessPolygons && MeshAnalysis == BooleanProcessing.IplicitSurfaceMethod.Grid);
		}
	}
}
