using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace PsyApi.Security
{
    public static class PasswordHasher
    {
        public static string Hash(string plain)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Argon2idHash(plain, salt, 4, 65536, 2, 32);
            return $"argon2id$mem=65536,iter=4,par=2${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string plain, string hash)
        {
            try
            {
                // Format: argon2id$mem=...,iter=...,par=...$salt$hash
                var parts = hash.Split('$');
                if (parts.Length == 4 && parts[0] == "argon2id")
                {
                    var param = parts[1];
                    var salt = Convert.FromBase64String(parts[2]);
                    var target = Convert.FromBase64String(parts[3]);

                    // parse params
                    int mem = 65536, iter = 4, par = 2;
                    foreach (var kv in param.Split(','))
                    {
                        var p = kv.Split('=');
                        if (p.Length != 2) continue;
                        if (p[0] == "mem" && int.TryParse(p[1], out var m)) mem = m;
                        if (p[0] == "iter" && int.TryParse(p[1], out var it)) iter = it;
                        if (p[0] == "par" && int.TryParse(p[1], out var pr)) par = pr;
                    }

                    var actual = Argon2idHash(plain, salt, iter, mem, par, target.Length);
                    return CryptographicOperations.FixedTimeEquals(actual, target);
                }
            }
            catch
            {
                // fall-through
            }
            return false;
        }

        private static byte[] Argon2idHash(string plain, byte[] salt, int iterations, int memoryKb, int parallelism, int hashLength)
        {
            var a = new Argon2id(Encoding.UTF8.GetBytes(plain))
            {
                Salt = salt,
                Iterations = iterations,
                MemorySize = memoryKb,
                DegreeOfParallelism = parallelism
            };
            return a.GetBytes(hashLength);
        }
    }
}

