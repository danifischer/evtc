using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Eto.Drawing;
using Eto.Forms;
using GW2Scratch.ArcdpsLogManager.Gw2Api;
using GW2Scratch.ArcdpsLogManager.Logs;
using GW2Scratch.ArcdpsLogManager.Logs.Naming;
using GW2Scratch.ArcdpsLogManager.Processing;
using GW2Scratch.ArcdpsLogManager.Sections;
using GW2Scratch.ArcdpsLogManager.Sections.Guilds;
using JetBrains.Annotations;

namespace GW2Scratch.ArcdpsLogManager.Controls
{
	public sealed class GuildDetailPanel : DynamicLayout, INotifyPropertyChanged
	{
		private static readonly GuildData NullGuild = new GuildData(null, new LogData[0], new LogPlayer[0]);

		private GuildData guildData = NullGuild;

		private ApiData ApiData { get; }
		private LogDataProcessor LogProcessor { get; }
		private UploadProcessor UploadProcessor { get; }
		private ImageProvider ImageProvider { get; }
		private ILogNameProvider LogNameProvider { get; }

		public GuildData GuildData
		{
			get => guildData;
			set
			{
				if (value == null)
				{
					value = NullGuild;
				}

				if (Equals(value, guildData)) return;
				guildData = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public GuildDetailPanel(ApiData apiData, LogDataProcessor logProcessor, UploadProcessor uploadProcessor,
			ImageProvider imageProvider, ILogNameProvider logNameProvider)
		{
			ImageProvider = imageProvider;
			LogNameProvider = logNameProvider;
			ApiData = apiData;
			LogProcessor = logProcessor;
			UploadProcessor = uploadProcessor;

			Padding = new Padding(10);
			Width = 350;
			Visible = false;

			var accountGridView = ConstructAccountGridView();
			var characterGridView = ConstructCharacterGridView();

			var tabs = new TabControl();
			tabs.Pages.Add(new TabPage(accountGridView) {Text = "Accounts"});
			tabs.Pages.Add(new TabPage(characterGridView) {Text = "Characters"});

			PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName != nameof(GuildData)) return;

				Visible = GuildData != NullGuild;
			};

			BeginVertical(spacing: new Size(0, 30));
			{
				BeginVertical();
				{
					Add(ConstructGuildNameLabel());
					Add(ConstructMemberCountLabel());
				}
				EndVertical();
				BeginVertical(yscale: true);
				{
					Add(tabs);
				}
				EndVertical();


				BeginVertical();
				{
					// TODO: Add a button to find logs with the currently selected account/character
					Add(ConstructLogListButton());
				}
				EndVertical();
			}
			EndVertical();
		}

		private Label ConstructGuildNameLabel()
		{
			var label = new Label()
			{
				Font = Fonts.Sans(16, FontStyle.Bold),
				Wrap = WrapMode.Word
			};
			PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName != nameof(GuildData)) return;

				string name = GuildData.Guid != null ? ApiData.GetGuildName(GuildData.Guid) : "(Unknown)";
				string tag = GuildData.Guid != null ? ApiData.GetGuildTag(GuildData.Guid) : "???";
				label.Text = $"{name} [{tag}]";
			};

			return label;
		}

		private Label ConstructMemberCountLabel()
		{
			var label = new Label
			{
				Font = Fonts.Sans(12),
				Wrap = WrapMode.Word
			};
			PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName != nameof(GuildData)) return;
				if (GuildData == null) return;

				string members = $"{GuildData.Accounts.Count} {(GuildData.Accounts.Count == 1 ? "member" : "members")}";
				string characters =
					$"{GuildData.Characters.Count} {(GuildData.Characters.Count == 1 ? "character" : "characters")}";
				label.Text = $"{members}, {characters}";
			};

			return label;
		}

		private Button ConstructLogListButton()
		{
			var button = new Button {Text = "Show logs with this guild"};
			button.Click += (sender, args) =>
			{
				var form = new Form
				{
					Content = new LogList(ApiData, LogProcessor, UploadProcessor, ImageProvider, LogNameProvider)
					{
						DataStore = new FilterCollection<LogData>(GuildData.Logs)
					},
					Width = 900,
					Height = 700,
					Title = $"arcdps Log Manager: logs with a member in {ApiData.GetGuildName(guildData.Guid)}"
				};
				form.Show();
			};

			return button;
		}

		private GridView<GuildCharacter> ConstructCharacterGridView()
		{
			var gridView = new GridView<GuildCharacter>();
			gridView.Columns.Add(new GridColumn
			{
				HeaderText = "Logs",
				DataCell = new TextBoxCell
					{Binding = new DelegateBinding<GuildCharacter, string>(x => $"{x.Logs.Count}")}
			});
			var classImageColumn = new GridColumn
			{
				HeaderText = "",
				DataCell = new ImageViewCell
				{
					Binding = new DelegateBinding<GuildCharacter, Image>(x =>
						ImageProvider.GetTinyProfessionIcon(x.Profession)),
				},
				Width = 20,
			};
			gridView.Columns.Add(classImageColumn);
			gridView.Columns.Add(new GridColumn
			{
				HeaderText = "Character",
				DataCell = new TextBoxCell
					{Binding = new DelegateBinding<GuildCharacter, string>(x => x.Name)}
			});
			gridView.Columns.Add(new GridColumn
			{
				HeaderText = "Account",
				DataCell = new TextBoxCell
					{Binding = new DelegateBinding<GuildCharacter, string>(x => x.Account.Name.Substring(1))}
			});

			var sorter = new GridViewSorter<GuildCharacter>(gridView,
				new Dictionary<GridColumn, Comparison<GuildCharacter>>
				{
					{classImageColumn, (left, right) => left.Profession.CompareTo(right.Profession)}
				});
			sorter.EnableSorting();

			PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName != nameof(GuildData)) return;
				gridView.DataStore = new FilterCollection<GuildCharacter>(GuildData?.Characters);
				sorter.UpdateDataStore();
			};

			return gridView;
		}

		private GridView<GuildMember> ConstructAccountGridView()
		{
			var gridView = new GridView<GuildMember>();
			gridView.Columns.Add(new GridColumn
			{
				HeaderText = "Logs",
				DataCell = new TextBoxCell
					{Binding = new DelegateBinding<GuildMember, string>(x => $"{x.Logs.Count}")}
			});
			gridView.Columns.Add(new GridColumn
			{
				HeaderText = "Account",
				DataCell = new TextBoxCell
					{Binding = new DelegateBinding<GuildMember, string>(x => x.Name.Substring(1))}
			});
			gridView.Columns.Add(new GridColumn
			{
				HeaderText = "Characters",
				DataCell = new TextBoxCell
					{Binding = new DelegateBinding<GuildMember, string>(x => $"{x.Characters.Count}")}
			});

			var sorter = new GridViewSorter<GuildMember>(gridView);
			sorter.EnableSorting();

			PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName != nameof(GuildData)) return;
				gridView.DataStore = new FilterCollection<GuildMember>(GuildData?.Accounts);
				sorter.UpdateDataStore();
			};

			return gridView;
		}

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}