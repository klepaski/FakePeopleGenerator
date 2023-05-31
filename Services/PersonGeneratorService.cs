using Bogus;
using task5.Models;
using CsvHelper;
using System.Globalization;
using System.Runtime.Serialization;

namespace task5.Services
{
    public enum Region
    {
        ru, en, ko
    }

    public interface IPersonGeneratorService
    {
        public PersonModel GeneratePerson(int seed, Region region);
        public List<PersonModel> GeneratePersons(int seed, Region region, int amountOfPersons, int amountOfMistakes);
        public MemoryStream GetPersonsInCsvFile();
    }

    public class PersonGeneratorService : IPersonGeneratorService
    {
        private Faker _faker = new();
        private int _currentSeed;
        private int _currentAmountOfMistakes;
        private List<PersonModel> _currentListOfPersons = new();

        private const string EN_CODE = "+1", RU_CODE = "+7", KO_CODE = "+82";
        private const int EN_MIN_PHONE = 501111111, EN_MAX_PHONE = 889999999;
        private const int RU_MIN_PHONE = 611111111, RU_MAX_PHONE = 789999999;
        private const int KO_MIN_PHONE = 21111111, KO_MAX_PHONE = 29999999;
        private const int BUILDING_NUMBER_MIN = 1, BUILDING_NUMBER_MAX = 150;
        private const int APARTMENT_NUMBER_MIN = 1, APARTMENT_NUMBER_MAX = 255;

        public PersonModel GeneratePerson(int seed, Region region)
        {
            Randomizer.Seed = new Random(seed);
            _faker.Locale = Enum.GetName(region.GetType(), region);
            string apartment = (_faker.Random.Number(10) < 5 ? $"-{_faker.Random.Number(APARTMENT_NUMBER_MIN, APARTMENT_NUMBER_MAX)}" : "");

            PersonModel person = new PersonModel()
            {
                Number = _faker.IndexFaker,
                Id = _faker.Random.Uuid().ToString(),
                Name = _faker.Name.FullName(),
                Address = $"{_faker.Address.City()}, {_faker.Address.StreetName()} {_faker.Random.Number(BUILDING_NUMBER_MIN, BUILDING_NUMBER_MAX)}{apartment}",
                Phone = GeneratePhoneNumber()
            };
            _faker.IndexFaker++;
            return person;
        }

        public List<PersonModel> GeneratePersons(int seed, Region region, int amountOfPersons, int amountOfMistakes)
        {
            ChangeFakerContext(region, seed, amountOfMistakes);
            for (int i = 0; i < amountOfPersons; i++)
            {
                PersonModel person = GeneratePerson(seed, region);
                _currentListOfPersons.Add(person);
            }
            return _currentListOfPersons;
        }

        private void ChangeFakerContext(Region region, int seed, int amountOfMistakes)
        {
            var currentRegion = Enum.GetName(region.GetType(), region);
            if (!_faker.Locale.Equals(currentRegion) || _currentSeed != seed || _currentAmountOfMistakes != amountOfMistakes)
            {
                _faker = new Faker(currentRegion);
                _faker.IndexFaker += 2;
                _currentSeed = seed;
                _currentAmountOfMistakes = amountOfMistakes;
                _currentListOfPersons = new();
            }
        }

        private string GeneratePhoneNumber()
        {
            Region currentRegion = (Region)Enum.Parse(typeof(Region), _faker.Locale);
            return (currentRegion switch
            {
                Region.en => $"({EN_CODE}) {_faker.Random.UInt(EN_MIN_PHONE, EN_MAX_PHONE)}",
                Region.ru => $"({RU_CODE}) {_faker.Random.UInt(RU_MIN_PHONE, RU_MAX_PHONE)}",
                Region.ko => $"({KO_CODE}) {_faker.Random.UInt(KO_MIN_PHONE, KO_MAX_PHONE)}",
                _ => string.Empty,
            });
        }

        public MemoryStream GetPersonsInCsvFile()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(_currentListOfPersons);
            stream.Position = 0;
            return stream;
        }
    }
}
