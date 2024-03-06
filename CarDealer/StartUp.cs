using AutoMapper;
using AutoMapper.Configuration.Annotations;
using AutoMapper.QueryableExtensions;
using CarDealer.Data;
using CarDealer.DTOs.Export;
using CarDealer.DTOs.Import;
using CarDealer.Models;
using Castle.Core.Resource;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace CarDealer
{
    public class StartUp
    {
        public static void Main()
        {
            CarDealerContext context = new CarDealerContext();

            //9. Import Suppliers
            //string inputSupplierXml = File.ReadAllText("../../../Datasets/suppliers.xml");
            //Console.WriteLine(ImportSuppliers(context,inputSupplierXml));

            //10. Import Parts
            //string inputPartsXml = File.ReadAllText("../../../Datasets/parts.xml");
            //Console.WriteLine(ImportParts(context, inputPartsXml));

            //11. Import Cars
            //string inputCarsXml = File.ReadAllText("../../../Datasets/cars.xml");
            //Console.WriteLine(ImportCars(context, inputCarsXml));

            //12.Import Customers
            //string inputCustomersXml = File.ReadAllText("../../../Datasets/customers.xml");
            //Console.WriteLine(ImportCustomers(context, inputCustomersXml));

            //Query 13. Import Sales
            //string inputSalesXml = File.ReadAllText("../../../Datasets/sales.xml");
            //Console.WriteLine(ImportSales(context, inputSalesXml));

            //14. Export Cars With Distance
            //Console.WriteLine(GetCarsWithDistance(context));

            //18. Export Total Sales by Customer
            Console.WriteLine(GetTotalSalesByCustomer(context));

            //Console.WriteLine(GetSalesWithAppliedDiscount(context));

        }

        private static Mapper GetMapper()
        {
            var cfg = new MapperConfiguration(c => c.AddProfile<CarDealerProfile>());

            return new Mapper(cfg);
        }
        private static string SerializeToXml<T>(T dto, string xmlRootAttribute)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T), new XmlRootAttribute(xmlRootAttribute));

            StringBuilder stringBuilder = new StringBuilder();

            using (StringWriter sw = new StringWriter(stringBuilder,CultureInfo.InvariantCulture))
            {
                XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
                xmlSerializerNamespaces.Add(string.Empty, string.Empty);

                try
                {
                    xmlSerializer.Serialize(sw, dto, xmlSerializerNamespaces);
                }
                catch (Exception)
                {

                    throw;
                }
            }
            return stringBuilder.ToString();
        }

        //9. Import Suppliers
        public static string ImportSuppliers(CarDealerContext context, string inputXml)
        {
            //1. Create xml serializer
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportSupplierDTO[]),
                new XmlRootAttribute("Suppliers"));

            //2.
            using var reader = new StringReader(inputXml);
            ImportSupplierDTO[] improtSupplierDTOs = (ImportSupplierDTO[])xmlSerializer.Deserialize(reader);

            //3. Mapper
            var mapper = GetMapper();
            Supplier[] suppliers = mapper.Map<Supplier[]>(improtSupplierDTOs);

            //4. Add to EF context
            context.AddRange(suppliers);

            //5. Commit changes to DB
            context.SaveChanges();

            return $"Successfully imported {suppliers.Length}";
        }

        //10. Import Parts
        public static string ImportParts(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportPartsDTO[]), 
                new XmlRootAttribute("Parts"));

            using StringReader reader = new StringReader(inputXml);
            ImportPartsDTO[] importPartsDTOs = (ImportPartsDTO[])xmlSerializer.Deserialize(reader);

            var supplierIds = context.Suppliers
                .Select(x=> x.Id)
                .ToArray();

            var mapper = GetMapper();

            Part[] parts = mapper.Map<Part[]>(importPartsDTOs.Where(p => supplierIds.Contains(p.SupplierId)));

            context.AddRange(parts);

            context.SaveChanges();

            return $"Successfully imported {parts.Length}";
        }

        //11. Import Cars
        public static string ImportCars(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportCarDTO[]),
                new XmlRootAttribute("Cars"));

            using StringReader reader = new StringReader(inputXml);

            ImportCarDTO[] importCarDTOs = (ImportCarDTO[])xmlSerializer.Deserialize(reader);

            var mapper = GetMapper();

            List<Car> cars = new List<Car>();

            foreach (var carsDTO in importCarDTOs)
            {
                Car car = mapper.Map<Car>(carsDTO);

                int[] carPartIds = carsDTO.PartsIds
                    .Select(x => x.Id)
                    .Distinct()
                    .ToArray();

                var carParts = new List<PartCar>();

                foreach (var id in carPartIds)
                {
                    carParts.Add(new PartCar
                    {
                        Car = car,
                        PartId = id
                    });
                }

                car.PartsCars = carParts;
                cars.Add(car);
            }

            context.AddRange(cars);
            context.SaveChanges();

            return $"Successfully imported {cars.Count}";
        }

        //12. Import Customers
        public static string ImportCustomers(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportCustomerDTO[]),
                new XmlRootAttribute("Customers"));

            StringReader reader = new StringReader(inputXml);

            ImportCustomerDTO[] importCustomerDTOs = (ImportCustomerDTO[])xmlSerializer.Deserialize(reader);

            var mapper = GetMapper();

            Customer[] customers = mapper.Map<Customer[]>(importCustomerDTOs);

            context.AddRange(customers);
            context.SaveChanges();

            return $"Successfully imported {customers.Length}";
        }

        //Query 13. Import Sales
        public static string ImportSales(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportSaleDTO[]),
                new XmlRootAttribute("Sales"));

            StringReader reader = new StringReader(inputXml);

            ImportSaleDTO[] importSaleDTOs = (ImportSaleDTO[])xmlSerializer.Deserialize(reader);

            var mapper = GetMapper();

            int[] carIds =context.Cars.Select(car => car.Id).ToArray();

            Sale[] sales = mapper.Map<Sale[]>(importSaleDTOs)
                .Where(s => carIds.Contains(s.CarId)).ToArray();

            context.AddRange(sales);
            context.SaveChanges();

            return $"Successfully imported {sales.Length}";
        }

        //14. Export Cars With Distance
        public static string GetCarsWithDistance(CarDealerContext context)
        {
            var mapper = GetMapper();

            var carsWithDistance = context.Cars
                .Where(c => c.TraveledDistance > 2000000)
                .OrderBy(c => c.Make)
                .ThenBy(c => c.Model)
                .Take(10)
                .ProjectTo<ExportCarsWithDistance>(mapper.ConfigurationProvider)
                .ToArray();

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportCarsWithDistance[])
                , new XmlRootAttribute("cars"));

            var xsn = new XmlSerializerNamespaces();
            xsn.Add(string.Empty, string.Empty);

            StringBuilder strBuilder = new StringBuilder();

            using(StringWriter sw = new StringWriter(strBuilder))
            {
                xmlSerializer.Serialize(sw, carsWithDistance, xsn);
            }

            return strBuilder.ToString().TrimEnd();
        }

        //18. Export Total Sales by Customer
        public static string GetTotalSalesByCustomer(CarDealerContext context)
        {
            var temp = context.Customers
                .Where(c => c.Sales.Any())
                .Select(c => new
                {
                    FullName = c.Name,
                    BoughtCars = c.Sales.Count(),
                    SalesInfo = c.Sales.Select(s => new
                    {
                        Prices = c.IsYoungDriver
                                ? s.Car.PartsCars.Sum(pc => Math.Round((double)pc.Part.Price * 0.95, 2))
                                : s.Car.PartsCars.Sum(pc => (double)pc.Part.Price)
                    })
                        .ToArray(),
                })
                .ToArray();


            ExportSalesPerCustomerDTO[] totalSales = temp.OrderByDescending(x => x.SalesInfo.Sum(y => y.Prices))
                .Select(x => new ExportSalesPerCustomerDTO()
                {
                    FullName = x.FullName,
                    BoughtCars = x.BoughtCars,
                    SpentMoney = x.SalesInfo.Sum(y => (decimal)y.Prices)
                })
                .ToArray();

            return SerializeToXml<ExportSalesPerCustomerDTO[]>(totalSales, "customers");
        }

        //Query 19. Export Sales with Applied Discount
        public static string GetSalesWithAppliedDiscount(CarDealerContext context)
        {
            ExportSaleAppliedDiscountDTO[] sales = context.Sales
                .Select(s => new ExportSaleAppliedDiscountDTO()
                {
                    Car = new ExportCarWithAttrDTO()
                    {
                        Make = s.Car.Make,
                        Model = s.Car.Model,
                        TraveledDistance = s.Car.TraveledDistance
                    },
                    Discount = (int)s.Discount,
                    CustomerName = s.Customer.Name,
                    Price = s.Car.PartsCars.Sum(p => p.Part.Price),
                    PriceWithDiscount =
                        Math.Round((double)(s.Car.PartsCars
                            .Sum(p => p.Part.Price) * (1 - (s.Discount / 100))), 4)
                })
                .ToArray();

            return SerializeToXml<ExportSaleAppliedDiscountDTO[]>(sales, "sales");
        }
    }
}