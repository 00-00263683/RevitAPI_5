using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlagin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = LevelSelect(doc, "Уровень 1");
            Level level2 = LevelSelect(doc, "Уровень 2");

            Transaction transaction = new Transaction(doc, "Построение");
            transaction.Start();
            List<Wall> walls = CreateWalls(doc, level1, level2);
            AddDoor(doc, level1, walls[0]);
            double height = 600;
            AddWindow(doc, level1, walls[1], height);
            AddWindow(doc, level1, walls[2], height);
            AddWindow(doc, level1, walls[3], height);
            transaction.Commit();

            return Result.Succeeded;
        }

        public Level LevelSelect(Document doc, string levelname)
        {
            List<Level> listlevel = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .OfType<Level>()
            .ToList();
            Level leveselect = listlevel
                            .Where(x => x.Name.Equals(levelname))
                            .OfType<Level>()
                            .FirstOrDefault();
            return leveselect;
        }
        public List<Wall> CreateWalls(Document doc, Level botton, Level top)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);

            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();

            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, botton.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(top.Id);
            }
            return walls;
        }
        private void AddDoor(Document doc, Level level, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Doors)
            .OfType<FamilySymbol>()
            .Where(x => x.Name.Equals("0915 x 2134 мм"))
            .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
            .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            if (!doorType.IsActive)
            {
                doorType.Activate();
            }
            doc.Create.NewFamilyInstance(point, doorType, wall, level, StructuralType.NonStructural);

        }
        private void AddWindow(Document doc, Level level, Wall wall, double height)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Windows)
            .OfType<FamilySymbol>()
            .Where(x => x.Name.Equals("0915 x 0610 мм"))
            .Where(x => x.FamilyName.Equals("Фиксированные"))
            .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            if (!windowType.IsActive)
            {
                windowType.Activate();
            }
            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level, StructuralType.NonStructural);
            
            double h = UnitUtils.ConvertToInternalUnits(height, UnitTypeId.Millimeters);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(h);            
        }
    }
}
