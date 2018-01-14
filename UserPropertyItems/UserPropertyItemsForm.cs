using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MoreLinq;
using Tekla.Structures;
using Tekla.Structures.Catalogs;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;

namespace UserPropertyItems
{
	public partial class UserPropertyItemsForm : Form
	{
		private readonly Model _model = new Model();

		public UserPropertyItemsForm()
		{
			InitializeComponent();
		}

		private void buttonAllBeams_Click(object sender, EventArgs e)
		{
			//get all model objects of type BEAM  (columns removed)
			ModelObjectEnumerator.AutoFetch = true;
			var beams = _model.GetAllBeams()
				.Where(b => b.Type == Beam.BeamTypeEnum.BEAM)  //remove columns
				.ToList();

			Process(beams);
		}

		private void buttonSelect_Click(object sender, EventArgs e)
		{
			var beams = _model.PickMultipleObjects()
				.OfType<Beam>()
				.Where(b => b.Type == Beam.BeamTypeEnum.BEAM)  //remove columns
				.ToList();

			Process(beams);
		}

		private void Process(List<Beam> beams)
		{
			//create list of custom user properties
			var names = new List<string>
			{
				"RAZORCX_TIMESTAMP",
				"RAZORCX_1",
				"RAZORCX_2",
				"RAZORCX_3",
				"RAZORCX_4"
			};

			//create the custom user properties for model objects of type BEAM
			CreateUserPropertyItem(names);

			//set user properties (could be anything, we are using beam properties for example)
			beams.ForEach(b =>
			{
				b.SetUserProperty("RAZORCX_TIMESTAMP", DateTime.UtcNow.ToString());
				b.SetUserProperty("RAZORCX_1", $"{b.Name} {b.Profile.ProfileString} {b.Material.MaterialString}");
				b.SetUserProperty("RAZORCX_2", $"{b.Profile.ProfileString}");
				b.SetUserProperty("RAZORCX_3", $"{b.Material.MaterialString}");
				b.SetUserProperty("RAZORCX_4", $"{b.Finish}");
			});

			//get user property item values and create anonymous list as datasource for datagridview
			var userPropertyItems = beams.Select(b =>
			{
				var values = new Hashtable();
				b.GetAllUserProperties(ref values);

				return new
				{
					RAZORCX_TIMESTAMP = values[names[0]],
					RAZORCX_1 = values[names[1]],
					RAZORCX_2 = values[names[2]],
					RAZORCX_3 = values[names[3]],
					RAZORCX_4 = values[names[4]],
				};
			}).ToList();

			dataGridView1.DataSource = userPropertyItems;

			//get user properties and output results to console
			beams.ForEach(b =>
			{
				var value = string.Empty;
				b.GetUserProperty("RAZORCX_TIMESTAMP", ref value);

				var values = new Hashtable();
				b.GetAllUserProperties(ref values);

				Console.WriteLine(value);

				values.OfType<DictionaryEntry>().ForEach(pair =>
				{
					Console.WriteLine($@"{pair.Key}: {values[pair.Key]}");
				});

				Console.WriteLine(@"------------------------------------------");
			});
		}

		private void CreateUserPropertyItem(List<string> names)
		{
			names.ForEach(name =>
			{
				var up = new UserPropertyItem
				{
					Name = name,
					Level = UserPropertyLevelEnum.LEVEL_MODEL,
					FieldType = UserPropertyFieldTypeEnum.FIELDTYPE_TEXT,
					Type = PropertyTypeEnum.TYPE_STRING,
					Visibility = UserPropertyVisibilityEnum.VISIBILITY_READONLY,
					Unique = true,
					AffectsNumbering = true
				};
				up.Insert();
				up.SetLabel(name);
				up.AddToObjectType(CatalogObjectTypeEnum.STEEL_BEAM);
			});
		}
	}

	public static class ExtentionMethods
	{
		public static List<Beam> GetAllBeams(this Model model)
		{
			return model.GetModelObjectSelector()
				.GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM)
				.ToAList<Beam>();
		}

		public static List<T> ToAList<T>(this IEnumerator enumerator)
		{
			var list = new List<T>();
			while (enumerator.MoveNext())
			{
				var current = (T)enumerator.Current;
				if (current != null)
					list.Add(current);
			}
			return list;
		}

		public static List<ModelObject> PickMultipleObjects(this Model model)
		{
			ModelObjectEnumerator.AutoFetch = true;

			try
			{
				var p = new Picker();
				var picked =
					p.PickObjects(Picker.PickObjectsEnum.PICK_N_OBJECTS,
						"Pick objects (single or multiple)")
						.ToAList<ModelObject>();
				return
					picked.GroupBy(c => c.Identifier.ID)
						.Select(g => g.FirstOrDefault())
						.ToList();
			}
			catch
			{
				return null;
			}
		}
	}
}
