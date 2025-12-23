import Combine
import SwiftUI

class ImageLoader: ObservableObject {
    @Published var image: NSImage?
    private let url: URL
    private var cancellable: AnyCancellable?

    // Shared URLCache with large disk capacity (500MB)
    static let cache: URLCache = {
        let cache = URLCache(memoryCapacity: 50 * 1024 * 1024, diskCapacity: 500 * 1024 * 1024)
        return cache
    }()

    init(url: URL) {
        self.url = url
    }

    func load() {
        // Check memory cache first (URLCache handles this transparently usually, but good to be explicit if using custom NSCache)
        // Here we rely on URLCache's persistent disk cache + memory cache.

        // Create request with cache policy
        let request = URLRequest(
            url: url, cachePolicy: .returnCacheDataElseLoad, timeoutInterval: 30)

        if let cachedResponse = ImageLoader.cache.cachedResponse(for: request),
            let cachedImage = NSImage(data: cachedResponse.data)
        {
            self.image = cachedImage
            return
        }

        cancellable = URLSession.shared.dataTaskPublisher(for: request)
            .map { (data, response) -> NSImage? in
                // Cache the response manually if needed, but URLSession usually does it if configured
                if let httpResponse = response as? HTTPURLResponse,
                    200...299 ~= httpResponse.statusCode
                {
                    ImageLoader.cache.storeCachedResponse(
                        CachedURLResponse(response: httpResponse, data: data), for: request)
                }
                return NSImage(data: data)
            }
            .replaceError(with: nil)
            .receive(on: DispatchQueue.main)
            .sink { [weak self] in self?.image = $0 }
    }

    func cancel() {
        cancellable?.cancel()
    }
}

struct CachedAsyncImage<Content: View, Placeholder: View>: View {
    @StateObject private var loader: ImageLoader
    private let content: (Image) -> Content
    private let placeholder: () -> Placeholder

    init(
        url: URL,
        @ViewBuilder content: @escaping (Image) -> Content,
        @ViewBuilder placeholder: @escaping () -> Placeholder
    ) {
        _loader = StateObject(wrappedValue: ImageLoader(url: url))
        self.content = content
        self.placeholder = placeholder
    }

    var body: some View {
        Group {
            if let nsImage = loader.image {
                content(Image(nsImage: nsImage))
            } else {
                placeholder()
            }
        }
        .onAppear {
            loader.load()
        }
        .onDisappear {
            loader.cancel()
        }
    }
}
