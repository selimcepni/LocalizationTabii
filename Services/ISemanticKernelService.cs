using Microsoft.SemanticKernel;

namespace LocalizationTabii.Services;
public interface ISemanticKernelService
{
    /// <summary>
    /// Belirtilen model tanımlayıcısına göre yapılandırılmış bir Kernel nesnesi oluşturur.
    /// </summary>
    /// <param name="modelIdentifier">Kullanıcının seçtiği model tanımlayıcısı</param>
    /// <returns>Kullanıma hazır bir Kernel nesnesi</returns>
    Kernel CreateKernelForModel(string modelIdentifier);

    /// <summary>
    /// Kullanılabilir modellerin listesini döndürür.
    /// </summary>
    /// <returns>Desteklenen model tanımlayıcılarının listesi</returns>
    List<string> GetAvailableModels();

    /// <summary>
    /// Model tanımlayıcısından insan okunabilir isim üretir.
    /// </summary>
    /// <param name="modelIdentifier">Model tanımlayıcısı</param>
    /// <returns>İnsan okunabilir model ismi</returns>
    string GetModelDisplayName(string modelIdentifier);
} 