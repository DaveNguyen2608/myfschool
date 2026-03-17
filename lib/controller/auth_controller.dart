import '../models/login_response.dart';
import '../repository/auth_repository.dart';

class AuthController {
  final AuthRepository _repository = AuthRepository();

  Future<LoginResponse> login({
    required String phoneNumber,
    required String password,
  }) async {
    return await _repository.login(
      phoneNumber: phoneNumber,
      password: password,
    );
  }

  Future<String> changePassword({
    required String username,
    required String currentPassword,
    required String newPassword,
    required String confirmPassword,
  }) async {
    return await _repository.changePassword(
      username: username,
      currentPassword: currentPassword,
      newPassword: newPassword,
      confirmPassword: confirmPassword,
    );
  }
}
