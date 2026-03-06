import '../models/login_response.dart';
import '../repository/auth_repository.dart';

class AuthController {
  final AuthRepository _repository = AuthRepository();

  Future<LoginResponse> login({
    required String username,
    required String password,
  }) async {
    return _repository.login(
      username: username,
      password: password,
    );
  }
}