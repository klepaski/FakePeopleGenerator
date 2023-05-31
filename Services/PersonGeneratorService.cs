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

        private const int MAX_SWITCH_PROBABILITY = 800;
        private const int MAX_DELETE_PROBABILITY = 850;
        private const int MAX_ADD_PROBABILITY = 999;

        private int _currentSwitchProbability = MAX_SWITCH_PROBABILITY;
        private int _currentDeleteProbability = MAX_SWITCH_PROBABILITY;
        private int _currentAddProbability = MAX_SWITCH_PROBABILITY;

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
                if (_currentAmountOfMistakes > 0)
                    CorruptData(person);
                _currentListOfPersons.Add(person);
            }
            return _currentListOfPersons;
        }

        private void CorruptData(PersonModel person)
        {
            for (int i = 0; i < _currentAmountOfMistakes; i++)
            {
                switch (_faker.Random.Int(0, 3))
                {
                    case 0: person.Id = AddMistakeToPersonData(person.Id); break;
                    case 1: person.Name = AddMistakeToPersonData(person.Name); break;
                    case 2: person.Address = AddMistakeToPersonData(person.Address); break;
                    case 3: person.Phone = AddMistakeToPersonData(person.Phone); break;
                }
            }
            _currentSwitchProbability = MAX_SWITCH_PROBABILITY;
            _currentDeleteProbability = MAX_DELETE_PROBABILITY;
            _currentAddProbability = MAX_ADD_PROBABILITY;
        }

        private string AddMistakeToPersonData(string dataToModify)
        {
            int rolledNum = _faker.Random.Int(1, MAX_ADD_PROBABILITY);
            int mistakeNumber = (rolledNum <= _currentSwitchProbability) ? 0 : (rolledNum <= _currentDeleteProbability) ? 1 : 2;
            return (mistakeNumber switch
            {
                0 => MakeSwitchedCharsMistake(dataToModify),
                1 => MakeDeletedCharMistake(dataToModify),
                2 => MakeAdditionalCharMistake(dataToModify),
                _ => "",
            });
        }

        private string MakeDeletedCharMistake(string data)
        {
            if (_currentDeleteProbability > _currentSwitchProbability + 1) _currentDeleteProbability--;
            int indexToDelete = _faker.Random.Int(0, data.Length - 1);
            data = data.Remove(indexToDelete, 1);
            return data;
        }

        private string MakeAdditionalCharMistake(string data)
        {
            if (_currentDeleteProbability < _currentAddProbability - 1) _currentDeleteProbability++;
            int indexToAdd = _faker.Random.Int(0, data.Length);
            string characterToAdd = _faker.Random.AlphaNumeric(1);
            data = data.Insert(indexToAdd, characterToAdd);
            return data;
        }

        private string MakeSwitchedCharsMistake(string data)
        {
            int firstIndex = _faker.Random.Int(0, data.Length - 2);
            int secondIndex = firstIndex + 1;
            string switchedPart = String.Join("", data[secondIndex], data[firstIndex]);
            data = data.Remove(firstIndex, 2).Insert(firstIndex, switchedPart);
            return data;
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
